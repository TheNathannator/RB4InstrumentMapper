using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Indicates that a new device has connected and is awaiting initialization.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct XboxArrival
    {
        public const byte CommandId = 0x02;

        public readonly ulong SerialNumber;
        public readonly ushort VendorId;
        public readonly ushort ProductId;
        private readonly ulong ignored1;
        private readonly ulong ignored2;
    }
}