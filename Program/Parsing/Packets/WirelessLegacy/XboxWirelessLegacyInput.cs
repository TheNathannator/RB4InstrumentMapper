using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// The input report header used by the wireless legacy adapter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxWirelessLegacyInputHeader
    {
        public const byte CommandId = 0x20;

        public ushort Buttons;
        public byte UserIndex;
        public XboxWirelessLegacyDeviceType DeviceType;
    }
}