namespace RB4InstrumentMapper.Core.Parsing
{
    internal interface IBackendClient
    {
        ushort VendorId { get; }
        ushort ProductId { get; }

        bool MapGuideButton { get; }

        XboxResult SendMessage(XboxMessage message);
        XboxResult SendMessage<T>(in XboxMessage<T> message) where T : unmanaged;
    }
}