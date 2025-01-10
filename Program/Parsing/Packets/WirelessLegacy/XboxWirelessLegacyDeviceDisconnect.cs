using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Notifies when a device has disconnected from a wireless legacy adapter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxWirelessLegacyDeviceDisconnect
    {
        public const byte CommandId = 0x23;

        public byte UserIndex;
    }
}