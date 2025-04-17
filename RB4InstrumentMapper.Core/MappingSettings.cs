using RB4InstrumentMapper.Core.Mapping;

namespace RB4InstrumentMapper.Core
{
    /// <summary>
    /// Settings for the parsing backend.
    /// </summary>
    public static class BackendSettings
    {
        /// <summary>
        /// The controller emulator to use.
        /// </summary>
        public static MappingMode MapperMode { get; set; } = MappingMode.NotSet;

        /// <summary>
        /// Whether to use hardware-accurate drum mappings (only applies to ViGEmBus mode).
        /// </summary>
        public static bool UseAccurateDrumMappings { get; set; } = false;

        /// <summary>
        /// Whether packets should be logged to the console.
        /// </summary>
        public static bool LogPackets { get; set; } = false;
        public static double RiffmasterSensitivity { get; set; } = 1.5;
    }
}