namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Backend for handling controllers via Pcap.
    /// </summary>
    public static class BackendSettings
    {
        /// <summary>
        /// Whether or not packets should be logged to the console.
        /// </summary>
        public static bool LogPackets { get; set; } = false;
    }
}