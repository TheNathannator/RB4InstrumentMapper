using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// TODO: Chunk headers need a minimum length of 6 bytes

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Command flag definitions.
    /// </summary>
    [Flags]
    internal enum XboxCommandFlags : byte
    {
        None = 0,
        NeedsAcknowledgement = 0x10,
        SystemCommand = 0x20,
        ChunkStart = 0x40,
        ChunkPacket = 0x80
    }

    /// <summary>
    /// Header data for a message.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxCommandHeader
    {
        public const int MinimumByteLength = 4;

        public const byte PrimaryClient = 0;

        public byte CommandId;
        public byte Flags_Client;
        public byte SequenceCount;
        public int DataLength;
        public int ChunkIndex;

        public XboxCommandFlags Flags
        {
            get => (XboxCommandFlags)(Flags_Client & 0xF0);
            set => Flags_Client = (byte)((byte)value & 0xF0 | Client);
        }

        public byte Client
        {
            get => (byte)(Flags_Client & 0x0F);
            set => Flags_Client = (byte)((byte)Flags | value & 0x0F);
        }

        public static bool TryParse(ReadOnlySpan<byte> data, out XboxCommandHeader header, out int bytesRead)
        {
            if (data.Length < MinimumByteLength)
            {
                // GameInput backend only logs the command ID for the header
                if (data.Length == 1)
                {
                    header = new XboxCommandHeader()
                    {
                        CommandId = data[0],
                        Client = PrimaryClient,
                    };
                    bytesRead = 1;
                    return true;
                }

                header = default;
                bytesRead = 0;
                return false;
            }

            // Command info
            header = new XboxCommandHeader()
            {
                CommandId = data[0],
                Flags_Client = data[1],
                SequenceCount = data[2],
            };
            bytesRead = MinimumByteLength - 1;

            // Message length
            if (!DecodeLEB128(data.Slice(bytesRead), out int dataLength, out int byteLength))
            {
                return false;
            }
            header.DataLength = dataLength;
            bytesRead += byteLength;

            // Chunk index/length
            if ((header.Flags & XboxCommandFlags.ChunkPacket) != 0)
            {
                if (!DecodeLEB128(data.Slice(bytesRead), out int chunkIndex, out byteLength))
                {
                    return false;
                }

                header.ChunkIndex = chunkIndex;
                bytesRead += byteLength;
            }

            return true;
        }

        public bool TryWriteToBuffer(Span<byte> buffer, out int bytesWritten)
        {
            bytesWritten = 0;
            if (buffer.Length < GetByteLength())
                return false;

            // Command info
            buffer[0] = CommandId;
            buffer[1] = Flags_Client;
            buffer[2] = SequenceCount;
            bytesWritten += MinimumByteLength - 1;

            // Message length
            if (!EncodeLEB128(buffer.Slice(bytesWritten), DataLength, out int byteLength))
                return false;

            bytesWritten += byteLength;

            // Chunk index/length
            if ((Flags & XboxCommandFlags.ChunkPacket) != 0)
            {
                if (!EncodeLEB128(buffer.Slice(bytesWritten), ChunkIndex, out byteLength))
                    return false;

                bytesWritten += byteLength;
            }

            return true;
        }

        public int GetByteLength()
        {
            int size = MinimumByteLength - 1;

            // Data length
            Span<byte> encodeBuffer = stackalloc byte[sizeof(int)];
            bool success = EncodeLEB128(encodeBuffer, DataLength, out int length);
            Debug.Assert(success, "Failed to get byte length for data length!");
            size += length;

            // Chunk index
            if ((Flags & XboxCommandFlags.ChunkPacket) != 0)
            {
                success = EncodeLEB128(encodeBuffer, ChunkIndex, out length);
                Debug.Assert(success, "Failed to get byte length for chunk index!");
                size += length;
            }

            return size;
        }

        // https://en.wikipedia.org/wiki/LEB128
        private static bool DecodeLEB128(ReadOnlySpan<byte> data, out int result, out int byteLength)
        {
            byteLength = 0;
            result = 0;

            if (data.IsEmpty)
            {
                return false;
            }

            // Decode variable-length length value
            // Sequence length is limited to 4 bytes
            byte value;
            do
            {
                value = data[byteLength];
                result |= (value & 0x7F) << (byteLength * 7);
                byteLength++;
            }
            while ((value & 0x80) != 0 && byteLength < sizeof(int));

            // Detect length sequences longer than 4 bytes
            if ((value & 0x80) != 0)
            {
                Debug.Fail($"Variable-length value is greater than 4 bytes! Buffer: {ParsingUtils.ToHexString(data)}");
                byteLength = 0;
                result = 0;
                return false;
            }

            return true;
        }

        private static bool EncodeLEB128(Span<byte> buffer, int value, out int byteLength)
        {
            byteLength = 0;
            if (buffer.IsEmpty)
                return false;

            // Encode the given value
            // Sequence length is limited to 4 bytes
            byte result;
            do
            {
                result = (byte)(value & 0x7F);
                if (value > 0x7F)
                {
                    result |= 0x80;
                    value >>= 7;
                }

                buffer[byteLength] = result;
                byteLength++;
            }
            while (value > 0x7F && byteLength < sizeof(int));

            // Detect values too large to encode
            if (value > 0x7F)
            {
                Debug.Fail($"Value to encode ({value}) is greater than allowed!");
                return false;
            }

            return true;
        }
    }
}