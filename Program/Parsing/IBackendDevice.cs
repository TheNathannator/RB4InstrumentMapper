using System;

namespace RB4InstrumentMapper.Parsing
{
    internal interface IBackendClient
    {
        ushort VendorId { get; }
        ushort ProductId { get; }

        bool MapGuideButton { get; }

        XboxResult SendMessage(XboxMessage message);
        XboxResult SendMessage<T>(XboxMessage<T> message) where T : unmanaged;
        XboxResult SendMessage(XboxCommandHeader header);
        XboxResult SendMessage<T>(XboxCommandHeader header, ref T data) where T : unmanaged;
        XboxResult SendMessage(XboxCommandHeader header, Span<byte> data);
    }
}