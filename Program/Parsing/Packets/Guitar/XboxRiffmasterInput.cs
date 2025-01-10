using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// An input report from a Riffmaster guitar.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxRiffmasterInput
    {
        public const byte CommandId = 0x20;

        public XboxGuitarInput Base;

        public short JoystickX;
        public short JoystickY;

        private byte systemButtons;

        public bool ShareButton => (systemButtons & 0x01) != 0;

        public bool JoystickClick => (Base.Buttons & (ushort)XboxGamepadButton.LeftStickPress) != 0
            && !Base.LowerFretsPressed; // Overlaps with the solo fret flag
    }
}