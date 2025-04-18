using System;

namespace RB4InstrumentMapper.Core.Parsing
{
    internal interface IBackendClient
    {
        ushort VendorId { get; }
        ushort ProductId { get; }

        bool MapGuideButton { get; }

        XboxResult SendMessage(XboxMessage message);
        XboxResult SendMessage(XboxCommandHeader header);
        XboxResult SendMessage<T>(XboxCommandHeader header, ref T data) where T : unmanaged;
        XboxResult SendMessage(XboxCommandHeader header, Span<byte> data);
    }
}