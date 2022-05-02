using System;
using System.Collections.Generic;
using SharpPcap;
using RB4InstrumentMapper.Parsing;

// This is in the regular namespace to keep the other packet parsing stuff from bogging up
// auto-completions when the code in this file is the only code that needs to be referenced elsewhere 
namespace RB4InstrumentMapper
{
    /// <summary>
    /// Emulated devices that can be parsed to.
    /// </summary>
    public enum ParsingMode
    {
        ViGEmBus = 1,
        vJoy = 2
    }

    /// <summary>
    /// Handles packets from a capture device.
    /// </summary>
    public static class PacketParser
    {
        /// <summary>
        /// Device IDs detected during Pcap.
        /// </summary>
        private static Dictionary<ulong, XboxDevice> pcapIds = new Dictionary<ulong, XboxDevice>();

        /// <summary>
        /// Gets or sets the current parsing mode.
        /// </summary>
        public static ParsingMode ParseMode { get; set; } = (ParsingMode)0;

        /// <summary>
        /// Whether or not capture has been started through StartCapture().
        /// </summary>
        private static bool captureStarted = false;

        /// <summary>
        /// Starts capture from a given device.
        /// </summary>
        public static bool StartCapture(ILiveDevice device)
        {
            // Disallow starting capture multiple times without first stopping capture
            if (device.Started == true)
            {
                return false;
            }

            // Disallow starting while ParseMode is not set yet
            if (ParseMode == (ParsingMode)0)
            {
                return false;
            }

            // Initialize selected device's static client
            switch (ParseMode)
            {
                case ParsingMode.ViGEmBus:
                    if (!VigemStatic.Initialize())
                    {
                        return false;
                    }
                    break;

                case ParsingMode.vJoy:
                    if (!VjoyStatic.Available)
                    {
                        return false;
                    }
                    break;

                default:
                    // Parse mode has not been set yet
                    return false;
            }

            // Open the device
            device.Open(new DeviceConfiguration()
            {
                Snaplen = 45, // Capture small packets
                Mode = DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, // Promiscuous mode with maximum speed
                ReadTimeout = 50 // Read timeout
            });

            // Configure event handlers
            device.OnPacketArrival += HandlePcapPacket;
            device.OnCaptureStopped += OnCaptureStop;
            
            // Start capture
            device.StartCapture();
            captureStarted = true;
            return true;
        }

        /// <summary>
        /// Handles a received Pcap packet.
        /// </summary>
        private static void HandlePcapPacket(object sender, PacketCapture packet)
        {
            // Disallow parsing of packets if StartCapture() hasn't been called yet
            if (!captureStarted)
            {
                return;
            }

            // Packet must be at least 30 bytes long
            if (packet.Data.Length < (Length.ReceiverHeader + Length.CommandHeader))
            {
                return;
            }

            // Get device ID
            ulong deviceId = (ulong)(
                 packet.Data[HeaderOffset.DeviceId + 5]        |
                (packet.Data[HeaderOffset.DeviceId + 4] << 8)  |
                (packet.Data[HeaderOffset.DeviceId + 3] << 16) |
                (packet.Data[HeaderOffset.DeviceId + 2] << 24) |
                (packet.Data[HeaderOffset.DeviceId + 1] << 32) |
                (packet.Data[HeaderOffset.DeviceId]     << 40)
            );

            try
            {
                // Check if ID has been encountered yet
                if (!pcapIds.ContainsKey(deviceId))
                {
                    pcapIds.Add(deviceId, new XboxDevice(ParseMode));
                    Console.WriteLine($"Encountered new device with ID {deviceId.ToString("X12")}");
                }

                // Strip off receiver header and send the data to be parsed
                pcapIds[deviceId].ParseCommand(packet.Data.Slice(Length.ReceiverHeader));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while handling packet: {e.GetFirstLine()}");
                Logging.LogException(e);

                // Stop capture
                (sender as ILiveDevice).Close();
                return;
            }
        }

        // TODO: Add libusb support

        /// <summary>
        /// Cleans up when capture stops.
        /// </summary>
        private static void OnCaptureStop(object sender, CaptureStoppedEventStatus status)
        {
            foreach (XboxDevice device in pcapIds.Values)
            {
                device.Close();
            }

            // Clear IDs list
            pcapIds.Clear();

            VigemStatic.Close();
            VjoyStatic.Close();

            captureStarted = false;
        }
    }
}
