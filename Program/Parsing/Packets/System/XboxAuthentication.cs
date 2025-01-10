namespace RB4InstrumentMapper.Parsing
{
    internal static class XboxAuthentication
    {
        public const byte CommandId = 0x06;

        public static readonly XboxMessage SuccessMessage = new XboxMessage()
        {
            Header = new XboxCommandHeader()
            {
                CommandId = CommandId,
                Flags = XboxCommandFlags.SystemCommand,
            },
            Data = new byte[] { 0x01, 0x00 },
        };
    }
}