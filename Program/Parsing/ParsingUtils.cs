using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Helper functions for parsing.
    /// </summary>
    internal static class ParsingUtils
    {
        public static string ToHexString(ReadOnlySpan<byte> buffer)
        {
            const string characters = "0123456789ABCDEF";

            if (buffer.IsEmpty)
                return "";

            Span<char> stringBuffer = stackalloc char[buffer.Length * 3];
            for (int i = 0; i < buffer.Length; i++)
            {
                byte value = buffer[i];
                int stringIndex = i * 3;
                stringBuffer[stringIndex] = characters[(value & 0xF0) >> 4];
                stringBuffer[stringIndex + 1] = characters[value & 0x0F];
                stringBuffer[stringIndex + 2] = '-';
            }
            // Exclude last '-'
            stringBuffer = stringBuffer.Slice(0, stringBuffer.Length - 1);

            return stringBuffer.ToString();
        }

        public static bool TryParseBytesFromHexString(ReadOnlySpan<char> input, out byte[] bytes)
        {
            bytes = null;
            if (input.IsEmpty)
                return false;

            // Determine number of bytes based on character count
            input = input.Trim(); // All whitespace must be removed for count to be correct
            int charCount = input.Length + 1; // + 1 to account for removed '-'
            int byteCount = Math.DivRem(charCount, 3, out int remainder);
            if (remainder != 0)
                return false;

            bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                int inputIndex = i * 3;
                if (!HexCharToNumber(input[inputIndex], out byte upper) ||
                    !HexCharToNumber(input[inputIndex + 1], out byte lower))
                    return false;

                bytes[i] = (byte)((upper << 4) | (lower & 0x0F));

                // Verify that '-' is present
                int dashIndex = inputIndex + 2;
                if (dashIndex < input.Length)
                {
                    char dashChar = input[dashIndex];
                    if (dashChar != '-' && dashChar != ' ')
                        return false;
                }
            }

            return true;
        }

        private static bool HexCharToNumber(char c, out byte b)
        {
            uint value = (uint)c - '0';
            if (value > 9)
            {
                const uint AsciiLowercaseFlag = 0x20;
                value = (c | AsciiLowercaseFlag) - 'a' + 0x0A;
                if (value > 0x0F)
                {
                    b = 0;
                    return false;
                }
            }

            b = (byte)value;
            return true;
        }

        // Re-implementation of MemoryMarshal.TryRead without the references check,
        // since we have the unmanaged constraint
        public static bool TryRead<T>(ReadOnlySpan<byte> source, out T value)
            where T : unmanaged
        {
            if (source.Length < Unsafe.SizeOf<T>())
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(source));
            return true;
        }

        /// <summary>
        /// Scales a byte to a short, starting from the negative end.
        /// </summary>
        public static short ScaleToInt16(this byte input)
        {
            return (short)(((input ^ 0x80) << 8) | input);
        }

        /// <summary>
        /// Scales a byte to an unsigned short.
        /// </summary>
        public static ushort ScaleToUInt16(this byte input)
        {
            return (ushort)((input << 8) | input);
        }
    }
}
