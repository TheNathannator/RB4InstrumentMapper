using System;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// A mapper which does nothing.
    /// </summary>
    internal class DummyMapper : DeviceMapper
    {
        public DummyMapper(IBackendClient client) : base(client) {}
        public override void ResetReport() {}
        protected override void MapGuideButton(bool pressed) {}
        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
            => XboxResult.Success;
    }
}