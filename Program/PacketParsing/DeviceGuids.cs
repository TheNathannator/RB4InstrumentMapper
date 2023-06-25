using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Xbox device interface GUIDs.
    /// </summary>
    internal static class DeviceGuids
    {
        public static readonly Guid XboxInputDevice = Guid.Parse("9776FF56-9BFD-4581-AD45-B645BBA526D6");
        public static readonly Guid XboxNavigationController = Guid.Parse("B8F31FE7-7386-40E9-A9F8-2F21263ACFB7");
        public static readonly Guid XboxGamepad = Guid.Parse("082E402C-07DF-45E1-A5AB-A3127AF197B5");

        public static readonly Guid MadCatzGuitar = Guid.Parse("0D2AE438-7F7D-4933-8693-30FC55018E77");
        public static readonly Guid MadCatzDrumkit = Guid.Parse("06182893-CCE0-4B85-9271-0A10DBAB7E07");
        public static readonly Guid PdpGuitar = Guid.Parse("1A266AF6-3A46-45E3-B9B6-0F2C0B2C1EBE");
        public static readonly Guid PdpDrumkit = Guid.Parse("A503F9B0-955E-47C4-A2ED-B1336FA7703E");
    }
}
