namespace RB4InstrumentMapper.Parsing
{
    public enum MappingMode
    {
        NotSet = 0,
        ViGEmBus = 1,
        vJoy = 2,
        RPCS3 = 3,
    }

    /// <summary>
    /// Backend for handling controllers via Pcap.
    /// </summary>
    public static class BackendSettings
    {
        /// <summary>
        /// The controller emulator to use.
        /// </summary>
        public static MappingMode MapperMode { get; set; } = MappingMode.NotSet;

        /// <summary>
        /// Whether or not packets should be logged to the console.
        /// </summary>
        public static bool LogPackets { get; set; } = false;
    }
}