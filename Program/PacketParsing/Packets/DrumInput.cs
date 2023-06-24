using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Re-definitions for button flags that have specific meanings.
    /// </summary>
    [Flags]
    internal enum DrumButton : ushort
    {
        // Not used as these are for menu navigation purposes
        // RedPad = GamepadButton.B,
        // GreenPad = GamepadButton.A,
        KickOne = GamepadButton.LeftBumper,
        KickTwo = GamepadButton.RightBumper
    }

    /// <summary>
    /// An input report from a drumkit.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DrumInput
    {
        /// <summary>
        /// Masks for each pad's value.
        /// </summary>
        enum DrumPad : ushort
        {
            Red = 0x00F0,
            Yellow = 0x000F,
            Blue = 0xF000,
            Green = 0x0F00
        }

        /// <summary>
        /// Masks for each cymbal's value.
        /// </summary>
        enum DrumCymbal : ushort
        {
            Yellow = 0x00F0,
            Blue = 0x000F,
            Green = 0xF000
        }

        public ushort Buttons;
        private readonly ushort pads;
        private readonly ushort cymbals;

        public byte RedPad => (byte)((pads & (ushort)DrumPad.Red) >> 4);
        public byte YellowPad => (byte)(pads & (ushort)DrumPad.Yellow);
        public byte BluePad => (byte)((pads & (ushort)DrumPad.Blue) >> 12);
        public byte GreenPad => (byte)((pads & (ushort)DrumPad.Green) >> 8);

        public byte YellowCymbal => (byte)((cymbals & (ushort)DrumCymbal.Yellow) >> 4);
        public byte BlueCymbal => (byte)(cymbals & (ushort)DrumCymbal.Blue);
        public byte GreenCymbal => (byte)((cymbals & (ushort)DrumCymbal.Green) >> 12);
    }
}