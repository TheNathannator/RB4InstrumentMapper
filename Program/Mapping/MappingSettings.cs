namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Settings for the parsing backend.
    /// </summary>
    public static class MappingSettings
    {
        /// <summary>
        /// Whether or not to use accurate drum mappings (only applies to ViGEmBus mode).
        /// </summary>
        public static bool UseAccurateDrumMappings { get; set; } = false;
    }
}