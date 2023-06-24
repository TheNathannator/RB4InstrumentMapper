using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    internal enum BatteryType : byte
    {
        Wired = 0,
        Standard = 1,
        ChargeKit = 2,
    }

    internal enum BatteryLevel : byte
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Full = 3,

        Wired = Low,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct DeviceStatus
    {
        private readonly byte status;
        private readonly byte unk1;
        private readonly byte unk2;
        private readonly byte unk3;

        public bool Connected => (status & 0b1100_0000) != 0;
        public BatteryType BatteryType => (BatteryType)(status & 0b0000_1100);
        public BatteryLevel BatteryLevel => (BatteryLevel)(status & 0b0000_0011);
    }
}