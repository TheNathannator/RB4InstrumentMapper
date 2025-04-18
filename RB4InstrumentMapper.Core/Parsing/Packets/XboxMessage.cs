using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Core.Parsing
{
    internal class XboxMessage
    {
        private XboxCommandHeader _header;
        private byte[] _bytes;

        public XboxCommandHeader Header
        {
            get => _header;
            set
            {
                _header = value;
                _header.DataLength = _bytes?.Length ?? 0;
            }
        }

        public byte[] Bytes
        {
            get => _bytes;
            set
            {
                _bytes = value;
                _header.DataLength = _bytes?.Length ?? 0;
            }
        }
    }

    internal unsafe class XboxMessage<TData> : XboxMessage
        where TData : unmanaged
    {
        public TData Data
        {
            get => MemoryMarshal.Read<TData>(Bytes);
            set => MemoryMarshal.Write(Bytes, ref value);
        }

        public XboxMessage()
        {
            Bytes = new byte[sizeof(TData)];
        }
    }
}