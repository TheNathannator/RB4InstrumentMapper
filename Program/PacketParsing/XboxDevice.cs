using System;
using System.Collections.Generic;

namespace RB4InstrumentMapper.Parsing
{
    public enum MappingMode
    {
        ViGEmBus = 1,
        vJoy = 2
    }

    public enum XboxResult
    {
        Success,
        Pending,
        Disconnected,
        InvalidMessage,
    }

    /// <summary>
    /// An Xbox device.
    /// </summary>
    public class XboxDevice : IDisposable
    {
        public static MappingMode MapperMode;

        /// <summary>
        /// The clients currently on the device.
        /// </summary>
        private readonly Dictionary<int, XboxClient> clients = new Dictionary<int, XboxClient>();

        ~XboxDevice()
        {
            Dispose(false);
        }

        /// <summary>
        /// Handles an incoming packet for this device.
        /// </summary>
        public unsafe XboxResult HandlePacket(ReadOnlySpan<byte> data)
        {
            // Some devices may send multiple messages in a single packet, placing them back-to-back
            // The header length is very important in these scenarios, as it determines which bytes are part of the message
            // and where the next message's header begins.
            while (data.Length > 0)
            {
                // Command header
                if (!CommandHeader.TryParse(data, out var header, out int headerLength))
                {
                    return XboxResult.InvalidMessage;
                }
                int messageLength = headerLength + header.DataLength;

                // Verify bounds
                if (data.Length < messageLength)
                {
                    return XboxResult.InvalidMessage;
                }

                var messageData = data.Slice(0, messageLength);
                var commandData = messageData.Slice(headerLength);

                if (!clients.TryGetValue(header.Client, out var client))
                {
                    client = new XboxClient();
                    clients.Add(header.Client, client);
                }
                var clientResult = client.HandleMessage(header, commandData);
                switch (clientResult)
                {
                    case XboxResult.Success:
                    case XboxResult.Pending:
                        break;
                    case XboxResult.Disconnected:
                        return clientResult;
                    default:
                        if (data.Length < 1)
                            return clientResult;
                        break;
                }

                // Progress to next message
                data = data.Slice(messageData.Length);
            }

            return XboxResult.Success;
        }

        /// <summary>
        /// Performs cleanup for the device.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var client in clients.Values)
                {
                    client.Dispose();
                }
            }
        }
    }
}