using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Available types of batteries that can be used on a controller.
    /// </summary>
    internal enum XboxBatteryType : byte
    {
        Wired = 0,
        Standard = 1,
        ChargeKit = 2,
    }

    /// <summary>
    /// The amount of battery remaining on the controller.
    /// </summary>
    internal enum XboxBatteryLevel : byte
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Full = 3,

        Wired = Low,
    }

    /// <summary>
    /// Provides information about a device's current status, such as battery type and level.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct XboxStatus
    {
        public const byte CommandId = 0x03;

        private readonly byte status;

        // The GHL guitar does not send these bytes,
        // so they are ignored since we don't recognize them anyways
        // private readonly byte unk1;
        // private readonly byte unk2;
        // private readonly byte unk3;

        public bool Connected => (status & 0b1100_0000) != 0;
        public XboxBatteryType BatteryType => (XboxBatteryType)((status & 0b0000_1100) >> 2);
        public XboxBatteryLevel BatteryLevel => (XboxBatteryLevel)(status & 0b0000_0011);
    }
}