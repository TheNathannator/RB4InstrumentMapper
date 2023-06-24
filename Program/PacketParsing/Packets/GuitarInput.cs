using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Re-definitions for button flags that have specific meanings.
    /// </summary>
    [Flags]
    internal enum GuitarButton : ushort
    {
        StrumUp = GamepadButton.DpadUp,
        StrumDown = GamepadButton.DpadDown,
        GreenFret = GamepadButton.A,
        RedFret = GamepadButton.B,
        YellowFret = GamepadButton.Y,
        BlueFret = GamepadButton.X,
        OrangeFret = GamepadButton.LeftBumper,
        LowerFretFlag = GamepadButton.LeftStickPress
    }

    /// <summary>
    /// Flags used in <see cref="GuitarInput.UpperFrets"/> and <see cref="GuitarInput.LowerFrets"/>
    /// </summary>
    [Flags]
    internal enum GuitarFret : byte
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
    internal struct GuitarInput
    {
        public ushort Buttons;
        public byte Tilt;
        public byte WhammyBar;
        public byte PickupSwitch;
        public byte UpperFrets;
        public byte LowerFrets;
        private readonly byte unk1;
        private readonly byte unk2;
        private readonly byte unk3;

        public bool Green => ((UpperFrets | LowerFrets) & (byte)GuitarFret.Green) != 0;
        public bool Red => ((UpperFrets | LowerFrets) & (byte)GuitarFret.Red) != 0;
        public bool Yellow => ((UpperFrets | LowerFrets) & (byte)GuitarFret.Yellow) != 0;
        public bool Blue => ((UpperFrets | LowerFrets) & (byte)GuitarFret.Blue) != 0;
        public bool Orange => ((UpperFrets | LowerFrets) & (byte)GuitarFret.Orange) != 0;

        public bool LowerFretFlag => (Buttons & (ushort)GuitarButton.LowerFretFlag) != 0;
    }
}