using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Core.Parsing
{
    internal static class XboxAuthentication
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Success
        {
            public byte subcommand;
            public byte unknown;
        }

        public const byte CommandId = 0x06;

        public static XboxMessage<Success> SuccessMessage => new XboxMessage<Success>()
        {
            CommandId = CommandId,
            Flags = XboxCommandFlags.SystemCommand,
            Data = new Success()
            {
                subcommand = 0x01,
                unknown = 0x00,
            },
        };
    }
}