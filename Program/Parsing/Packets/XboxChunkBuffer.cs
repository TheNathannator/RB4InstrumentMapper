using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Parsing
{
    internal class XboxChunkBuffer
    {
        public byte[] Buffer { get; private set; }
        public int BytesUsed { get; private set; }
        public int BytesRemaining => Buffer != null ? Buffer.Length - BytesUsed : 0;

        public XboxResult ProcessChunk(ref XboxCommandHeader header, ref ReadOnlySpan<byte> chunkData)
        {
            int bufferIndex = header.ChunkIndex;

            // Do nothing with chunks of length 0
            if (bufferIndex <= 0)
            {
                // Chunked packets with a length of 0 are valid and have been observed with Elite controllers
                bool emptySequence = bufferIndex == 0;
                Debug.Assert(emptySequence, $"Negative buffer index {bufferIndex}!");
                return emptySequence ? XboxResult.Success : XboxResult.InvalidMessage;
            }

            // Start of the chunk sequence
            if (Buffer == null || (header.Flags & XboxCommandFlags.ChunkStart) != 0)
            {
                // Safety check
                if ((header.Flags & XboxCommandFlags.ChunkStart) == 0)
                {
                    // Some devices trigger this condition during authentication,
                    // so we don't fail if it's an auth packet
                    Debug.Assert(header.CommandId == XboxAuthentication.CommandId,
                        "Invalid chunk sequence start! No chunk buffer exists, expected a chunk start packet");
                    return XboxResult.InvalidMessage;
                }

                // Buffer index is the total size of the buffer on the starting packet
                Buffer = new byte[bufferIndex];
                bufferIndex = 0;
                BytesUsed = 0;
            }

            // Validate sequence alignment
            if (bufferIndex != BytesUsed)
            {
                // We don't fail here since this seems to be a consistent issue on devices it affects
                // Debug.Fail("Invalid chunk sequence ordering! Buffer index is not aligned with the previous chunk");
                return XboxResult.InvalidMessage;
            }

            // Buffer index equalling buffer length signals the end of the sequence
            if (bufferIndex >= Buffer.Length)
            {
                // Safety checks
                if (bufferIndex > Buffer.Length)
                {
                    Debug.Fail("Invalid chunk sequence end! Buffer index is beyond the end of the chunk buffer");
                    return XboxResult.InvalidMessage;
                }

                if (chunkData.Length != 0)
                {
                    Debug.Fail("Invalid chunk sequence end! Data was provided beyond the end of the buffer");
                    return XboxResult.InvalidMessage;
                }

                // Send off finished chunk buffer
                chunkData = Buffer;
                Buffer = null;
                BytesUsed = 0;

                // Update header
                header.DataLength = chunkData.Length;
                header.Flags &= ~(XboxCommandFlags.ChunkPacket | XboxCommandFlags.ChunkStart);
                return XboxResult.Success;
            }

            // Verify chunk data bounds
            if ((bufferIndex + chunkData.Length) > Buffer.Length)
            {
                Debug.Fail($"Invalid chunk sequence! Data was provided beyond the end of the buffer");
                return XboxResult.InvalidMessage;
            }

            // Copy data to buffer
            chunkData.CopyTo(Buffer.AsSpan(bufferIndex, chunkData.Length));
            BytesUsed = bufferIndex + chunkData.Length;
            return XboxResult.Pending;
        }
    }
}