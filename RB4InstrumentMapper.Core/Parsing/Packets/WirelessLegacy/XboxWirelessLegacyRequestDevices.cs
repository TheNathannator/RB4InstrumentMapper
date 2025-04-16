namespace RB4InstrumentMapper.Core.Parsing
{
    /// <summary>
    /// Requests info for devices currently connected to a wireless legacy adapter.
    /// </summary>
    internal static class XboxWirelessLegacyRequestDevices
    {
        public const byte CommandId = 0x24;

        public static readonly XboxMessage RequestDevices = new XboxMessage()
        {
            Header = new XboxCommandHeader()
            {
                CommandId = CommandId,
                Flags = XboxCommandFlags.None,
            },
            // No data
        };
    }
}