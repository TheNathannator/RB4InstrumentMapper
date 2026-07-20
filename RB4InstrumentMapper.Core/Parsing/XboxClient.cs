using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RB4InstrumentMapper.Core.Mapping;

namespace RB4InstrumentMapper.Core.Parsing
{
    /// <summary>
    /// A logical client on an Xbox device.
    /// </summary>
    internal class XboxClient : IDisposable, IBackendClient
    {
        /// <summary>
        /// The parent device of the client.
        /// </summary>
        public XboxDevice Parent { get; }

        /// <summary>
        /// The arrival info of the client.
        /// </summary>
        public XboxArrival Arrival { get; private set; }

        /// <summary>
        /// The descriptor of the client.
        /// </summary>
        public XboxDescriptor Descriptor { get; private set; }

        /// <summary>
        /// The ID of the client.
        /// </summary>
        public byte ClientId { get; }

        ushort IBackendClient.VendorId => Arrival.VendorId;
        ushort IBackendClient.ProductId => Arrival.ProductId;

        bool IBackendClient.MapGuideButton => Parent.MapGuideButton;

        private DeviceMapper deviceMapper;

        private int descriptorFailCount = 0;

        private readonly Dictionary<byte, byte> previousReceiveSequence = new Dictionary<byte, byte>();
        private readonly Dictionary<byte, byte> previousSendSequence = new Dictionary<byte, byte>();
        private readonly Dictionary<byte, XboxChunkBuffer> chunkBuffers = new Dictionary<byte, XboxChunkBuffer>()
        {
            { XboxDescriptor.CommandId, new XboxChunkBuffer() },
        };

        public XboxClient(XboxDevice parent, byte clientId)
        {
            Parent = parent;
            ClientId = clientId;
        }

        ~XboxClient()
        {
            Dispose(false);
        }

        /// <summary>
        /// Parses command data from a packet.
        /// </summary>
        internal unsafe XboxResult HandleMessage(XboxCommandHeader header, ReadOnlySpan<byte> commandData)
        {
            // Verify packet length
            if (header.DataLength != commandData.Length)
            {
                Debug.Fail($"Command header length does not match buffer length! Header: {header.DataLength}  Buffer: {commandData.Length}");
                return XboxResult.InvalidMessage;
            }

            // Ensure acknowledgement happens regardless of pending/failure
            try
            {
                // Chunked packets
                if ((header.Flags & XboxCommandFlags.ChunkPacket) != 0)
                {
                    if (!chunkBuffers.TryGetValue(header.CommandId, out var chunkBuffer))
                    {
                        chunkBuffer = new XboxChunkBuffer();
                        chunkBuffers.Add(header.CommandId, chunkBuffer);
                    }

                    var chunkResult = chunkBuffer.ProcessChunk(ref header, ref commandData);
                    switch (chunkResult)
                    {
                        case XboxResult.Success:
                            break;
                        case XboxResult.Pending: // Chunk is unfinished
                            return chunkResult;
                        default: // Error handling the chunk
                            // Hack for descriptor read errors
                            if (header.CommandId == XboxDescriptor.CommandId)
                            {
                                descriptorFailCount++;
                                if (descriptorFailCount >= 3)
                                {
                                    // Disconnect device after too many failed descriptors
                                    return XboxResult.UnsupportedDevice;
                                }

                                var resendResult = SendMessage(XboxDescriptor.GetDescriptor);
                                if (resendResult != XboxResult.Success)
                                    return resendResult;
                            }
                            return chunkResult;
                    }
                }
            }
            finally
            {
                // Acknowledgement
                if ((header.Flags & XboxCommandFlags.NeedsAcknowledgement) != 0)
                {
                    SendAcknowledge(ref header, commandData);
                }
            }

            if (header.SequenceCount is {} incomingSequence)
            {
                // Don't handle the same packet twice
                if (previousReceiveSequence.TryGetValue(header.CommandId, out byte previousSequence) &&
                    incomingSequence == previousSequence)
                {
                    return XboxResult.Success;
                }

                previousReceiveSequence[header.CommandId] = incomingSequence;
            }

            // Handle packet
            return (header.Flags & XboxCommandFlags.SystemCommand) != 0
                ? HandleSystemCommand(header.CommandId, commandData)
                : HandleMapperCommand(header.CommandId, commandData);
        }

        private XboxResult HandleSystemCommand(byte commandId, ReadOnlySpan<byte> commandData)
        {
            switch (commandId)
            {
                case XboxArrival.CommandId:
                    return HandleArrival(commandData);

                case XboxStatus.CommandId:
                    return HandleStatus(commandData);

                case XboxDescriptor.CommandId:
                    return HandleDescriptor(commandData);

                case XboxKeystroke.CommandId:
                    return HandleKeystroke(commandData);
            }

            return XboxResult.Success;
        }

        private XboxResult HandleMapperCommand(byte commandId, ReadOnlySpan<byte> commandData)
        {
            // Skip if inputs are disabled
            if (!Parent.InputsEnabled)
                return XboxResult.Success;

            if (deviceMapper == null)
            {
                deviceMapper = MapperFactory.GetFallbackMapper(this);
                if (deviceMapper == null)
                {
                    // No more devices available, do nothing
                    return XboxResult.Success;
                }

                Logging.WriteLine("Warning: This device was not encountered during its initial connection! It will use the fallback mapper instead of one specific to its device interface.");
                Logging.WriteLine("Reconnect it (or hit Start before connecting it) to ensure correct behavior.");
            }

            return deviceMapper.HandleMessage(commandId, commandData);
        }

        /// <summary>
        /// Handles the arrival message of the device.
        /// </summary>
        private unsafe XboxResult HandleArrival(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxArrival arrival))
                return XboxResult.InvalidMessage;

            Logging.WriteLineVerbose($"New client connected with ID {arrival.SerialNumber:X12}");
            Arrival = arrival;

            // Kick off descriptor request
            return SendMessage(XboxDescriptor.GetDescriptor);
        }

        /// <summary>
        /// Handles the arrival message of the device.
        /// </summary>
        private unsafe XboxResult HandleStatus(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxStatus status))
                return XboxResult.InvalidMessage;

            if (!status.Connected)
                return XboxResult.Disconnected;

            return XboxResult.Success;
        }

        /// <summary>
        /// Handles the Xbox One descriptor of the device.
        /// </summary>
        private XboxResult HandleDescriptor(ReadOnlySpan<byte> data)
        {
            if (!XboxDescriptor.Parse(data, out var descriptor))
                return XboxResult.InvalidMessage;

            Descriptor = descriptor;

            if (Parent.InputsEnabled)
            {
                deviceMapper?.Dispose();
                deviceMapper = MapperFactory.GetByInterfaceIds(this, Descriptor.InterfaceGuids);
            }

            // Send final set of initialization messages
            Debug.Assert(Descriptor.OutputCommands.Contains(XboxConfiguration.CommandId));
            var result = SendMessage(XboxConfiguration.PowerOnDevice);
            if (result != XboxResult.Success)
                return result;

            if (Descriptor.OutputCommands.Contains(XboxLedControl.CommandId))
            {
                result = SendMessage(XboxLedControl.EnableLed);
                if (result != XboxResult.Success)
                    return result;
            }

            if (Descriptor.OutputCommands.Contains(XboxAuthentication.CommandId))
            {
                // Authentication is not and will not be implemented, we just automatically pass all devices
                result = SendMessage(XboxAuthentication.SuccessMessage);
                if (result != XboxResult.Success)
                    return result;
            }

            return XboxResult.Success;
        }

        private unsafe XboxResult HandleKeystroke(ReadOnlySpan<byte> data)
        {
            if (data.Length % sizeof(XboxKeystroke) != 0)
                return XboxResult.InvalidMessage;

            // Multiple keystrokes can be sent in a single message
            var keys = MemoryMarshal.Cast<byte, XboxKeystroke>(data);
            foreach (var key in keys)
            {
                deviceMapper?.HandleKeystroke(key);
            }

            return XboxResult.Success;
        }

        public XboxCommandHeader MakeHeader(byte commandId, XboxCommandFlags flags)
        {
            if (!previousSendSequence.TryGetValue(commandId, out byte sequence) ||
                sequence == 0xFF) // Sequence IDs of 0 are not valid
            {
                sequence = 0;
            }

            byte sequenceCount = ++sequence;
            previousSendSequence[commandId] = sequence;

            return new XboxCommandHeader()
            {
                CommandId = commandId,
                Flags = flags,
                Client = ClientId,
                SequenceCount = sequenceCount,
            };
        }

        public XboxResult SendMessage(XboxMessage message)
        {
            var header = MakeHeader(message.CommandId, message.Flags);
            return SendMessage(header, Span<byte>.Empty);
        }

        public XboxResult SendMessage<T>(in XboxMessage<T> message)
            where T : unmanaged
        {
            var header = MakeHeader(message.CommandId, message.Flags);
            return SendMessage(header, message.Data);
        }

        private unsafe XboxResult SendMessage<T>(XboxCommandHeader header, in T data)
            where T : unmanaged
        {
            var span = new Span<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(data)), sizeof(T));
            return SendMessage(header, span);
        }

        private XboxResult SendMessage(XboxCommandHeader header, Span<byte> data)
        {
            return Parent.SendMessage(header, data);
        }

        private XboxResult SendAcknowledge(ref XboxCommandHeader header, ReadOnlySpan<byte> commandData)
        {
            var (ackHeader, acknowledge) = chunkBuffers.TryGetValue(header.CommandId, out var chunkBuffer)
                ? XboxProtocolControl.MakeAck(header, chunkBuffer)
                : XboxProtocolControl.MakeAck(header, commandData);

            // Don't go through SetUpHeader, we must preserve the sequence ID in the header
            header.Client = ClientId;
            header.Flags &= ~XboxCommandFlags.NeedsAcknowledgement;

            return SendMessage(ackHeader, acknowledge);
        }

        public void EnableInputs(bool enabled)
        {
            deviceMapper?.Dispose();
            deviceMapper = null;

            if (enabled && Descriptor != null)
                deviceMapper = MapperFactory.GetByInterfaceIds(this, Descriptor.InterfaceGuids);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                deviceMapper?.Dispose();
                deviceMapper = null;
            }
        }
    }
}