using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    internal enum XboxLedMode : byte
    {
        Off = 0x00,
        On = 0x01,
        BlinkFast = 0x02,
        BlinkNormal = 0x03,
        BlinkSlow = 0x04,
        FadeSlow = 0x08,
        FadeFast = 0x09,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxLedControl
    {
        public const byte CommandId = 0x0a;

        public static readonly XboxMessage<XboxLedControl> EnableLed = new XboxMessage<XboxLedControl>()
        {
            Header = new XboxCommandHeader()
            {
                CommandId = CommandId,
                Flags = XboxCommandFlags.SystemCommand,
            },
            Data = new XboxLedControl()
            {
                Mode = XboxLedMode.On,
                Brightness = 0x14
            }
        };

        private byte unknown;

        public XboxLedMode Mode;
        public byte Brightness; 
    }
}
