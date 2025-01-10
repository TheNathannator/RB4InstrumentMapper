#if DEBUG
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// An input report from a gamepad.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxGamepadRumble
    {
        public enum Flags : byte
        {
            RightRumble = 0x01,
            LeftRumble = 0x02,
            RightTrigger = 0x04,
            LeftTrigger = 0x08,
        }

        public const byte CommandId = 0x09;

        private byte unknown;
        private Flags flags;

        public byte leftTrigger;
        public byte rightTrigger;
        public byte leftRumble;
        public byte rightRumble;

        public byte duration;
        public byte delay;
        public byte repeat;

        public static XboxMessage<XboxGamepadRumble> Create(byte left, byte right)
            => new XboxMessage<XboxGamepadRumble>()
        {
            Header = new XboxCommandHeader()
            {
                CommandId = CommandId,
                Flags = XboxCommandFlags.None,
            },
            Data = new XboxGamepadRumble()
            {
                flags = Flags.LeftRumble | Flags.RightRumble,
                leftRumble = left,
                rightRumble = right,

                duration = 0xFF,
                repeat = 0xEB,
            },
        };
    }

}
#endif