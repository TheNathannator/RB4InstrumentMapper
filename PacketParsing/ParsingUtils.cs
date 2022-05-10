using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Helper functions for parsing.
    /// </summary>
    static class ParsingUtils
    {
        /// <summary>
        /// Scales this byte to an int, starting from the negative end.
        /// </summary>
        public static int ScaleToInt32(this byte input)
        {
            // Duplicate the input value to the higher 8-bit regions by multiplying by a number with the
            // first bit of each region set to 1, then XOR with the negative bit to make the range start from the negative end
            return (int)((input * 0x01010101) ^ 0x80000000);
        }

        /// <summary>
        /// Scales this byte to a uint.
        /// </summary>
        public static uint ScaleToUInt32(this byte input)
        {
            // Duplicate the input value to the higher 8-bit regions by multiplying by a number with the
            // first bit of each region set to 1
            return (uint)(input * 0x01010101);
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

        /// <summary>
        /// Gets an unsigned short value from a specified index, parsed as a little-endian value.
        /// </summary>
        public static ushort GetUInt16LE(this ReadOnlySpan<byte> span, int index)
        {
            return (ushort)(span[index + 1] << 8 | span[index]);
        }

        /// <summary>
        /// Gets an unsigned short value from a specified index, parsed as a big-endian value.
        /// </summary>
        public static ushort GetUInt16BE(this ReadOnlySpan<byte> span, int index)
        {
            return (ushort)(span[index] << 8 | span[index + 1]);
        }

        /// <summary>
        /// Gets a short value from a specified index, parsed as a little-endian value.
        /// </summary>
        public static short GetInt16LE(this ReadOnlySpan<byte> span, int index)
        {
            return (short)(span[index + 1] << 8 | span[index]);
        }

        /// <summary>
        /// Gets a short value from a specified index, parsed as a big-endian value.
        /// </summary>
        public static short GetInt16BE(this ReadOnlySpan<byte> span, int index)
        {
            return (short)(span[index] << 8 | span[index + 1]);
        }

        /// <summary>
        /// Converts a ReadOnlySpan<byte> to a string.
        /// </summary>
        public static string ToHexString(this ReadOnlySpan<byte> span)
        {
            string hexString = ParsingHelpers.ByteArrayToHexString(span.ToArray());
            return hexString;
        }
    }
}
