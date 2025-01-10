using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Button flags for GHL guitars.
    /// </summary>
    [Flags]
    internal enum XboxGHLGuitarButton : ushort
    {
        White1 = 0x0001,
        Black1 = 0x0002,
        Black2 = 0x0004,
        Black3 = 0x0008,
        White2 = 0x0010,
        White3 = 0x0020,

        Select = 0x0100,
        Start = 0x0200,
        GHTV = 0x0400,

        // Already handled by the guide button messages
        // DpadCenter = 0x1000,
    }

    /// <summary>
    /// D-pad states for GHL guitars.
    /// </summary>
    public enum XboxGHLGuitarDpad : byte
    {
        // vJoy continuous PoV hat values range from 0 to 35999 (measured in 1/100 of a degree).
        // The value is measured clockwise, with up being 0.
        Neutral = 0x0F,
        Up = 0,
        UpRight = 1,
        Right = 2,
        DownRight = 3,
        Down = 4,
        DownLeft = 5,
        Left = 6,
        UpLeft = 7
    }

    /// <summary>
    /// An input report from a guitar.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 27)]
    internal struct XboxGHLGuitarInput
    {
        public const byte CommandId = 0x21;

        public const byte StrumbarCenter = 0x80;

        // For reference; PS3 stick axes put up as 0x00 and down as 0xFF
        // public const byte StrumbarUp = 0x00;
        // public const byte StrumbarDown = 0xFF;

        [FieldOffset(0)]
        public XboxGHLGuitarButton Buttons;

        [FieldOffset(2)]
        public XboxGHLGuitarDpad Dpad;

        [FieldOffset(4)]
        public byte StrumBar;

        [FieldOffset(6)]
        public byte WhammyBar;

        [FieldOffset(19)]
        public byte Tilt;

        public bool Black1 => (Buttons & XboxGHLGuitarButton.Black1) != 0;
        public bool Black2 => (Buttons & XboxGHLGuitarButton.Black2) != 0;
        public bool Black3 => (Buttons & XboxGHLGuitarButton.Black3) != 0;
        public bool White1 => (Buttons & XboxGHLGuitarButton.White1) != 0;
        public bool White2 => (Buttons & XboxGHLGuitarButton.White2) != 0;
        public bool White3 => (Buttons & XboxGHLGuitarButton.White3) != 0;

        public bool HeroPower => (Buttons & XboxGHLGuitarButton.Select) != 0;
        public bool Pause => (Buttons & XboxGHLGuitarButton.Start) != 0;
        public bool GHTV => (Buttons & XboxGHLGuitarButton.GHTV) != 0;

        // public bool DpadCenter => (Buttons & XboxGHLGuitarButton.DpadCenter) != 0;

        public bool DpadUp => Dpad == XboxGHLGuitarDpad.Up || Dpad == XboxGHLGuitarDpad.UpLeft || Dpad == XboxGHLGuitarDpad.UpRight;
        public bool DpadDown => Dpad == XboxGHLGuitarDpad.Down || Dpad == XboxGHLGuitarDpad.DownLeft || Dpad == XboxGHLGuitarDpad.DownRight;
        public bool DpadLeft => Dpad == XboxGHLGuitarDpad.Left || Dpad == XboxGHLGuitarDpad.UpLeft || Dpad == XboxGHLGuitarDpad.DownLeft;
        public bool DpadRight => Dpad == XboxGHLGuitarDpad.Right || Dpad == XboxGHLGuitarDpad.UpRight || Dpad == XboxGHLGuitarDpad.DownRight;

        public bool StrumUp => StrumBar < StrumbarCenter;
        public bool StrumDown => StrumBar > StrumbarCenter;
    }
}