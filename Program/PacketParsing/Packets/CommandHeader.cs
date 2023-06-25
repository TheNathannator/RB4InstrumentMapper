using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Command ID definitions.
    /// </summary>
    internal enum CommandId : byte
    {
        Acknowledgement = 0x01,
        Arrival = 0x02,
        Status = 0x03,
        Descriptor = 0x04,
        Authentication = 0x06,
        Keystroke = 0x07,
        SerialNumber = 0x1E,
        Input = 0x20,
    }

    /// <summary>
    /// Command flag definitions.
    /// </summary>
    [Flags]
    internal enum CommandFlags : byte
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
    internal struct CommandHeader
    {
        public CommandId CommandId;
        public CommandFlags Flags;
        public int Client;
        public byte SequenceCount;
        public int DataLength;
        public int ChunkIndex;

        public static bool TryParse(ReadOnlySpan<byte> data, out CommandHeader header, out int bytesRead)
        {
            header = default;
            bytesRead = 0;
            if (data == null || data.Length < 4)
            {
                return false;
            }

            // Command info
            header = new CommandHeader()
            {
                CommandId = (CommandId)data[0],
                Flags = (CommandFlags)(data[1] & 0xF0),
                Client = data[1] & 0x0F,
                SequenceCount = data[2],
            };
            bytesRead += 3;

            // Message length
            if (!ParsingUtils.DecodeLEB128(data.Slice(bytesRead), out int dataLength, out int byteLength))
            {
                return false;
            }
            header.DataLength = dataLength;
            bytesRead += byteLength;

            // Chunk index/length
            if ((header.Flags & CommandFlags.ChunkPacket) != 0)
            {
                if (!ParsingUtils.DecodeLEB128(data.Slice(bytesRead), out int chunkIndex, out byteLength))
                {
                    return false;
                }

                header.ChunkIndex = chunkIndex;
                bytesRead += byteLength;
            }

            return true;
        }
    }
}