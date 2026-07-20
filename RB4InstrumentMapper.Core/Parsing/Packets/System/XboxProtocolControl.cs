using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Core.Parsing
{
    /// <summary>
    /// Acknowledges a prior command and provides info about current buffer allocations.
    /// </summary>
    /// <remarks>
    /// Used for communication reliability and error detection.
    /// </remarks>
    internal static class XboxProtocolControl
    {
        public const byte CommandId = 0x01;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Acknowledge
        {
            public const byte ControlCodeId = 0x00;

            public byte ControlCode;
            public byte RefMessageType;
            public byte RefFlags_Expansion;
            public uint BytesReceived;
            public uint RemainingBuffer;

            public XboxCommandFlags RefFlags
            {
                // Docs specify that only the system flag is part of the ack
                get => (XboxCommandFlags)RefFlags_Expansion & XboxCommandFlags.SystemCommand;
                set => RefFlags_Expansion = (byte)((byte)(value & XboxCommandFlags.SystemCommand) | RefExpansion);
            }

            public byte RefExpansion
            {
                get => (byte)(RefFlags_Expansion & 0x07);
                set => RefFlags_Expansion = (byte)((byte)RefFlags | value & 0x07);
            }
        }

        public static (XboxCommandHeader ackHeader, Acknowledge ack) MakeAck(
            XboxCommandHeader originalHeader
        )
        {
            var ackHeader = new XboxCommandHeader()
            {
                CommandId = CommandId,
                Flags = XboxCommandFlags.SystemCommand,
                Client = originalHeader.Client,
                SequenceCount = originalHeader.SequenceCount,
                // Set when sending the packet
            };

            var ack = new Acknowledge()
            {
                ControlCode = Acknowledge.ControlCodeId,
                RefMessageType = originalHeader.CommandId,
                RefFlags = originalHeader.Flags,
                RefExpansion = originalHeader.Client,
                BytesReceived = 0,
                RemainingBuffer = 0,
            };

            return (ackHeader, ack);
        }

        public static (XboxCommandHeader ackHeader, Acknowledge ack) MakeAck(
            XboxCommandHeader originalHeader,
            ReadOnlySpan<byte> messageBuffer
        )
        {
            var (ackHeader, ack) = MakeAck(originalHeader);

            ack.BytesReceived = (uint)messageBuffer.Length;

            return (ackHeader, ack);
        }

        public static (XboxCommandHeader ackHeader, Acknowledge ack) MakeAck(
            XboxCommandHeader originalHeader,
            XboxChunkBuffer chunkBuffer = null
        )
        {
            var (ackHeader, ack) = MakeAck(originalHeader);

            if (chunkBuffer?.Buffer != null)
            {
                ack.BytesReceived = (uint)chunkBuffer.BytesUsed;
                ack.RemainingBuffer = (ushort)chunkBuffer.BytesRemaining;
            }

            return (ackHeader, ack);
        }
    }
}