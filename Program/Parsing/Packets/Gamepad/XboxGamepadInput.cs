using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Flag definitions for the buttons bytes.
    /// </summary>
    [Flags]
    internal enum XboxGamepadButton : ushort
    {
        Sync = 0x0001,
        // Unused = 0x0002,
        Menu = 0x0004,
        Options = 0x0008,
        A = 0x0010,
        B = 0x0020,
        X = 0x0040,
        Y = 0x0080,
        DpadUp = 0x0100,
        DpadDown = 0x0200,
        DpadLeft = 0x0400,
        DpadRight = 0x0800,
        LeftBumper = 0x1000,
        RightBumper = 0x2000,
        LeftStickPress = 0x4000,
        RightStickPress = 0x8000
    }

#if DEBUG

    /// <summary>
    /// An input report from a gamepad.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxGamepadInput
    {
        public const byte CommandId = 0x20;

        public const ushort TriggerMax = 0x03FF;

        public bool A => (Buttons & (ushort)XboxGamepadButton.A) != 0;
        public bool B => (Buttons & (ushort)XboxGamepadButton.B) != 0;
        public bool X => (Buttons & (ushort)XboxGamepadButton.X) != 0;
        public bool Y => (Buttons & (ushort)XboxGamepadButton.Y) != 0;

        public bool DpadUp => (Buttons & (ushort)XboxGamepadButton.DpadUp) != 0;
        public bool DpadDown => (Buttons & (ushort)XboxGamepadButton.DpadDown) != 0;
        public bool DpadLeft => (Buttons & (ushort)XboxGamepadButton.DpadLeft) != 0;
        public bool DpadRight => (Buttons & (ushort)XboxGamepadButton.DpadRight) != 0;

        public bool LeftBumper => (Buttons & (ushort)XboxGamepadButton.LeftBumper) != 0;
        public bool RightBumper => (Buttons & (ushort)XboxGamepadButton.RightBumper) != 0;
        public bool LeftStickPress => (Buttons & (ushort)XboxGamepadButton.LeftStickPress) != 0;
        public bool RightStickPress => (Buttons & (ushort)XboxGamepadButton.RightStickPress) != 0;

        public bool Menu => (Buttons & (ushort)XboxGamepadButton.Menu) != 0;
        public bool Options => (Buttons & (ushort)XboxGamepadButton.Options) != 0;

        public ushort Buttons;
        public ushort LeftTrigger;
        public ushort RightTrigger;
        public short LeftStickX;
        public short LeftStickY;
        public short RightStickX;
        public short RightStickY;
    }

#endif
}