using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Flags for keystroke events.
    /// </summary>
    internal enum XboxKeystrokeFlags : byte
    {
        Pressed = 0x01,
    }

    /// <summary>
    /// Possible key codes.
    /// </summary>
    /// <remarks>
    /// These mirror those in the Win32 API; for brevity, only the ones used are defined here.
    /// </remarks>
    internal enum XboxKeyCode : byte
    {
        LeftWindows = 0x5B, // Used for the guide button
    }

    /// <summary>
    /// A keystroke event from a controller.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxKeystroke
    {
        public const byte CommandId = 0x07;

        public XboxKeystrokeFlags Flags;
        public XboxKeyCode Keycode;

        public bool Pressed => (Flags & XboxKeystrokeFlags.Pressed) != 0;
    }
}