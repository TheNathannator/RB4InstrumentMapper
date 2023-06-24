using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Helper functions for parsing.
    /// </summary>
    internal static class ParsingUtils
    {
        // https://en.wikipedia.org/wiki/LEB128
        public static bool DecodeLEB128(ReadOnlySpan<byte> data, out int result, out int byteLength)
        {
            byteLength = 0;
            result = 0;

            if (data == null || data.Length < 1)
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
                Debug.WriteLine($"Variable-length value is greater than 4 bytes! Buffer: {BitConverter.ToString(data.ToArray())}");
                byteLength = 0;
                result = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Scales a byte to a short, starting from the negative end.
        /// </summary>
        public static short ScaleToInt16(this byte input)
        {
            // Duplicate the input value to the higher 8-bit regions by multiplying by a number with the
            // first bit of each region set to 1, then XOR with the negative bit to make the range start from the negative end
            return (short)((input * 0x0101) ^ 0x8000);
        }

        /// <summary>
        /// Scales a byte to an unsigned short.
        /// </summary>
        public static ushort ScaleToUInt16(this byte input)
        {
            // Duplicate the input value to the higher 8-bit regions by multiplying by a number with the
            // first bit of each region set to 1
            return (ushort)(input * 0x0101);
        }
    }
}
