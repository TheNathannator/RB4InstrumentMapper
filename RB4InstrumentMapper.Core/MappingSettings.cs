using RB4InstrumentMapper.Core.Mapping;

namespace RB4InstrumentMapper.Core
{
    /// <summary>
    /// Settings for the parsing backend.
    /// </summary>
    public static class BackendSettings
    {
        private static uint _pollingFrequency = 60;

        /// <summary>
        /// The controller emulator to use.
        /// </summary>
        public static MappingMode MapperMode { get; set; } = MappingMode.NotSet;

        /// <summary>
        /// The rate at which to poll for inputs, in hertz.
        /// </summary>
        public static uint PollingFrequency
        {
            get => _pollingFrequency;
            set => _pollingFrequency = ClampPollingFrequency(value);
        }

        /// <summary>
        /// Whether to use hardware-accurate drum mappings (only applies to ViGEmBus mode).
        /// </summary>
        public static bool UseAccurateDrumMappings { get; set; } = false;

        /// <summary>
        /// Whether packets should be logged to the console.
        /// </summary>
        public static bool LogPackets { get; set; } = false;

        /// <summary>
        /// Clamps the polling frequency value to an acceptable range.
        /// </summary>
        public static uint ClampPollingFrequency(uint frequency)
        {
            // This upper bound *would* be 1000, but in testing the
            // SleepTimer implementation can't do more than around 500hz
            if (frequency > 500)
            {
                return 500;
            }
            else if (frequency < 60)
            {
                return 60;
            }
            else
            {
                return frequency;
            }
        }
    }
}