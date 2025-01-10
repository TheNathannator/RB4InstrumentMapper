using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Acknowledges a prior command and provides info about current buffer allocations.
    /// </summary>
    /// <remarks>
    /// Used for communication reliability and error detection.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxAcknowledgement
    {
        public const byte CommandId = 0x01;

        private byte unk1;
        public byte InnerCommand;
        public byte InnerFlags_Client;
        public ushort BytesReceived;
        private ushort unk2;
        public ushort RemainingBuffer;

        public XboxCommandFlags InnerFlags
        {
            get => (XboxCommandFlags)(InnerFlags_Client & 0xF0);
            set => InnerFlags_Client = (byte)((byte)value & 0xF0 | InnerClient);
        }

        public byte InnerClient
        {
            get => (byte)(InnerFlags_Client & 0x0F);
            set => InnerFlags_Client = (byte)((byte)InnerFlags | value & 0x0F);
        }

        public static (XboxCommandHeader header, XboxAcknowledgement acknowledge) FromMessage(XboxCommandHeader header,
            ReadOnlySpan<byte> messageBuffer)
        {
            // The Xbox One driver seems to always send this for the inner flag
            header.Flags = XboxCommandFlags.SystemCommand;

            // Set acknowledgement data
            var acknowledge = new XboxAcknowledgement()
            {
                unk1 = 0,
                InnerCommand = header.CommandId,
                InnerFlags_Client = header.Flags_Client,
                unk2 = 0,
                BytesReceived = (ushort)messageBuffer.Length,
            };

            // Set remaining header data (length is set when sending)
            header.CommandId = CommandId;

            return (header, acknowledge);
        }

        public static (XboxCommandHeader header, XboxAcknowledgement acknowledge) FromMessage(XboxCommandHeader header,
            ReadOnlySpan<byte> messageBuffer, XboxChunkBuffer chunkBuffer)
        {
            var pair = FromMessage(header, messageBuffer);

            if (chunkBuffer.Buffer != null)
            {
                pair.acknowledge.BytesReceived = (ushort)chunkBuffer.BytesUsed;
                pair.acknowledge.RemainingBuffer = (ushort)chunkBuffer.BytesRemaining;
            }

            return pair;
        }
    }
}