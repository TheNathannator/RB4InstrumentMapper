using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Parsing
{
    internal enum PacketDirection
    {
        In,
        Out,
    }

    internal static class PacketLogging
    {
        private const string inStr = "->";
        private const string outStr = "<-";

        public static void WriteLine(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
            Logging.Packet_WriteLine(message);
        }

        public static void WritePacket(ReadOnlySpan<byte> header, ReadOnlySpan<byte> data, PacketDirection direction)
        {
            if (!BackendSettings.LogPackets)
                return;

            var time = DateTime.Now;
            int length = header.Length + data.Length;
            string headerStr = ParsingUtils.ToHexString(header);
            string dataStr = ParsingUtils.ToHexString(data);
            string directionStr = direction == PacketDirection.In ? inStr : outStr;
            WriteLine($"[{time:yyyy-MM-dd hh:mm:ss.fff}] [{length:D2}] {directionStr} {headerStr} | {dataStr}");
        }

        public static bool TryParsePacket(ReadOnlySpan<char> input,
            out byte[] header, out byte[] data, out PacketDirection direction)
        {
            header = Array.Empty<byte>();
            data = Array.Empty<byte>();
            direction = PacketDirection.In;
            if (input.IsEmpty)
                return false;

            // Skip time and packet length
            // For easier manual packet log construction, these are optional
            int lastBracket = input.LastIndexOf(']');
            if (lastBracket >= 0)
                input = input.Slice(++lastBracket).TrimStart();

            // Parse direction
            // For easier manual packet log construction, this defaults to in
            if (input.StartsWith(inStr.AsSpan()))
            {
                direction = PacketDirection.In;
                input = input.Slice(inStr.Length).TrimStart();
            }
            else if (input.StartsWith(outStr.AsSpan()))
            {
                input = input.Slice(outStr.Length).TrimStart();
                direction = PacketDirection.Out;
            }

            // Find header separator
            int headerSeparator = input.LastIndexOf('|');
            if (headerSeparator >= 0)
            {
                // Parse data (optional)
                // Not all packets have data
                var dataText = input.Slice(headerSeparator + 1);
                if (!ParsingUtils.TryParseBytesFromHexString(dataText, out data))
                    return false;
                input = input.Slice(0, headerSeparator);
            }

            // Parse header
            if (!ParsingUtils.TryParseBytesFromHexString(input, out header))
                return false;

            return true;
        }
    }
}