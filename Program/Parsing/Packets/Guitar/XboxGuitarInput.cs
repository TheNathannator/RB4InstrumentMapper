using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Re-definitions for button flags that have specific meanings.
    /// </summary>
    [Flags]
    internal enum XboxGuitarButton : ushort
    {
        StrumUp = XboxGamepadButton.DpadUp,
        StrumDown = XboxGamepadButton.DpadDown,
        GreenFret = XboxGamepadButton.A,
        RedFret = XboxGamepadButton.B,
        YellowFret = XboxGamepadButton.Y,
        BlueFret = XboxGamepadButton.X,
        OrangeFret = XboxGamepadButton.LeftBumper,
        LowerFretFlag = XboxGamepadButton.LeftStickPress
    }

    /// <summary>
    /// Flags used in <see cref="XboxGuitarInput.UpperFrets"/> and <see cref="XboxGuitarInput.LowerFrets"/>
    /// </summary>
    [Flags]
    internal enum XboxGuitarFret : byte
    {
        Green = 0x01,
        Red = 0x02,
        Yellow = 0x04,
        Blue = 0x08,
        Orange = 0x10
    }

    /// <summary>
    /// An input report from a guitar.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxGuitarInput
    {
        public const byte CommandId = 0x20;

        public ushort Buttons;
        public byte Tilt;
        public byte WhammyBar;
        public byte PickupSwitch;
        public byte UpperFrets;
        public byte LowerFrets;
        private readonly byte unk1;
        private readonly byte unk2;
        private readonly byte unk3;

        public bool Green => ((UpperFrets | LowerFrets) & (byte)XboxGuitarFret.Green) != 0;
        public bool Red => ((UpperFrets | LowerFrets) & (byte)XboxGuitarFret.Red) != 0;
        public bool Yellow => ((UpperFrets | LowerFrets) & (byte)XboxGuitarFret.Yellow) != 0;
        public bool Blue => ((UpperFrets | LowerFrets) & (byte)XboxGuitarFret.Blue) != 0;
        public bool Orange => ((UpperFrets | LowerFrets) & (byte)XboxGuitarFret.Orange) != 0;

        public bool LowerFretsPressed => LowerFrets != 0;
    }
}