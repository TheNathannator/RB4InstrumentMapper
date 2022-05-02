using System;
using System.Collections.Generic;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Definitions for the receiver header.
    /// </summary>
    static class Length
    {
        public const int
        ReceiverHeader = 26,
        CommandHeader = 4,
        VirtualKey = 2,
        Input_Gamepad = 0x0C,
        Input_Guitar = 0x0A,
        Input_Drums = 0x06;
    }

    /// <summary>
    /// Definitions for the receiver header.
    /// </summary>
    static class HeaderOffset
    {
        public const int
        DeviceId = 10;
    }

    /// <summary>
    /// Command header offsets relative to the end of the receiver header.
    /// </summary>
    static class CommandOffset
    {
        public const int
        CommandId = 0,
        Flags = 1,
        SequenceCount = 2,
        DataLength = 3;
    }

    /// <summary>
    /// Command IDs to be parsed.
    /// </summary>
    static class CommandId
    {
        public const int
        VirtualKey = 0x07,
        Input = 0x20;
    }

    /// <summary>
    /// Virtual keycodes that will be recognized.
    /// </summary>
    static class Keycodes
    {
        public const byte LeftWin = 0x5b;
    }

    /// <summary>
    /// Virtual keycode packet offsets relative to the end of the command header.
    /// </summary>
    static class KeycodeOffset
    {
        public const int
        PressedState = 0,
        Keycode = 1;
    }

    /// <summary>
    /// Flag definitions for the buttons bytes.
    /// </summary>
    static class GamepadButton
    {
        public const ushort
        DpadUp = 0x01,
        DpadDown = 0x02,
        DpadLeft = 0x04,
        DpadRight = 0x08,
        LeftBumper = 0x10,
        RightBumper = 0x20,
        LeftStickPress = 0x40,
        RightStickPress = 0x80,

        // Nothing useful can be done with this
        // Sync = 0x0100,
        // No known use for this bit
        // Unused = 0x0200,

        Menu = 0x0400,
        Options = 0x0800,
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000;
    }

    static class GamepadOffset
    {
        public const int
        Buttons = 0,
        LeftTrigger = 2,
        RightTrigger = 4,
        LeftStickX = 6,
        LeftStickY = 8,
        RightStickX = 10,
        RightStickY = 12;
    }

    /// <summary>
    /// Guitar input data offsets relative to the end of the command header.
    /// </summary>
    static class GuitarOffset
    {
        public const int
        Buttons = 0,
        Tilt = 2,
        WhammyBar = 3,
        PickupSwitch = 4,
        UpperFrets = 5,
        LowerFrets = 6;

        // Final 3 bytes are uknown
    }

    static class GuitarButton
    {
        public const ushort
        StrumUp = GamepadButton.DpadUp,
        StrumDown = GamepadButton.DpadDown,
        GreenFret = GamepadButton.A,
        RedFret = GamepadButton.B,
        YellowFret = GamepadButton.Y,
        BlueFret = GamepadButton.X,
        OrangeFret = GamepadButton.LeftBumper,
        LowerFretFlag = GamepadButton.LeftStickPress;
    }

    /// <summary>
    /// Flag definitions for the guitar fret bytes.
    /// </summary>
    static class GuitarFret
    {
        public const byte
        Green = 0x01,
        Red = 0x02,
        Yellow = 0x04,
        Blue = 0x08,
        Orange = 0x10;
    }

    /// <summary>
    /// Drums input data offsets relative to the end of the command header.
    /// </summary>
    static class DrumOffset
    {
        public const int
        Buttons = 0,
        PadVels = 2,
        CymbalVels = 4;
    }

    static class DrumButton
    {
        public const ushort
        RedPad = GamepadButton.B,
        YellowPad = GamepadButton.Y,
        BluePad = GamepadButton.X,
        GreenPad = GamepadButton.A,
        KickOne = GamepadButton.LeftBumper,
        KickTwo = GamepadButton.RightBumper;
    }

    /// <summary>
    /// Definitions for drumkit pad velocity data.
    /// </summary>
    static class DrumPadVel
    {
        public const byte
        Red = 0xF0,
        Yellow = 0x0F,
        Blue = 0xF0,
        Green = 0x0F;
    }

    /// <summary>
    /// Definitions for drumkit pad velocity data.
    /// </summary>
    static class DrumCymVel
    {
        public const byte
        // No red cymbal
        // Red = 0,
        Yellow = 0xF0,
        Blue = 0x0F,
        Green = 0xF0;
    }
}