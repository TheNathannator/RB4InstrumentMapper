namespace RB4InstrumentMapper.Parsing
{
    internal class XboxMessage
    {
        private XboxCommandHeader _header;
        private byte[] _data;

        public XboxCommandHeader Header
        {
            get => _header;
            set
            {
                _header = value;
                _header.DataLength = _data?.Length ?? 0;
            }
        }

        public byte[] Data
        {
            get => _data;
            set
            {
                _data = value;
                _header.DataLength = _data?.Length ?? 0;
            }
        }
    }

    internal unsafe class XboxMessage<TData>
        where TData : unmanaged
    {
        private XboxCommandHeader _header;
        public TData Data;

        public XboxCommandHeader Header
        {
            get => _header;
            set
            {
                _header = value;
                _header.DataLength = sizeof(TData);
            }
        }
    }
}