namespace RB4InstrumentMapper.Core.Parsing
{
    internal ref struct XboxMessage
    {
        public byte CommandId;
        public XboxCommandFlags Flags;
    }

    internal ref struct XboxMessage<TData>
        where TData : unmanaged
    {
        public byte CommandId;
        public XboxCommandFlags Flags;

        public TData Data;
    }
}