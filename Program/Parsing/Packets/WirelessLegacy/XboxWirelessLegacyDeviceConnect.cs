using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Possible device types for the wireless legacy adapter.
    /// </summary>
    internal enum XboxWirelessLegacyDeviceType : byte
    {
        Guitar = 1,
        Drums = 2,
    }

    /// <summary>
    /// Possible subtypes for XInput devices.
    /// </summary>
    internal enum XInputDeviceSubtype : byte
    {
        Unknown = 0,
        Gamepad = 1,
        Wheel = 2,
        ArcadeStick = 3,
        FlightStick = 4,
        DancePad = 5,
        Guitar = 6,
        GuitarAlternate = 7,
        Drums = 8,
        GuitarBass = 11,
        Keyboard = 15,
        ArcadePad = 19,
        Turntable = 23,
    }

    /// <summary>
    /// Reports info about a device that has just connected to a wireless legacy adapter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct XboxWirelessLegacyDeviceConnect
    {
        public const byte CommandId = 0x22;

        public const int MinimumLength = 6;

        public byte UserIndex;
        public XboxWirelessLegacyDeviceType DeviceType;

        private ushort vendorId; // big-endian
        private byte unknown;
        private byte xinputSubype;

        private fixed char name[124];

        public ushort VendorId => (ushort)((vendorId & 0xFF) << 8 | (vendorId & 0xFF00) >> 8);
        public XInputDeviceSubtype XInputSubType => (XInputDeviceSubtype)(xinputSubype & 0x7F);

        public static bool TryParse(ReadOnlySpan<byte> buffer, out XboxWirelessLegacyDeviceConnect connectInfo)
        {
            connectInfo = new XboxWirelessLegacyDeviceConnect();

            if (buffer.Length < MinimumLength)
                return false;

            // Create a byte buffer reference and copy the message buffer into it
            var writeBuffer = new Span<byte>(Unsafe.AsPointer(ref connectInfo), sizeof(XboxWirelessLegacyDeviceConnect));
            if (buffer.Length > sizeof(XboxWirelessLegacyDeviceConnect))
                buffer = buffer.Slice(0, sizeof(XboxWirelessLegacyDeviceConnect));

            return buffer.TryCopyTo(writeBuffer);
        }
    }
}