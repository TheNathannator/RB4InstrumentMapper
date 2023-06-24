using System;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Vigem;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// A mapper that maps to a ViGEmBus device.
    /// </summary>
    internal abstract class VigemMapper : IDeviceMapper
    {
        public bool MapGuideButton { get; set; } = false;

        /// <summary>
        /// The device to map to.
        /// </summary>
        protected IXbox360Controller device;

        /// <summary>
        /// Whether or not the emulated Xbox 360 controller has connected fully.
        /// </summary>
        protected bool deviceConnected = false;

        public VigemMapper()
        {
            device = VigemClient.CreateDevice();
            device.FeedbackReceived += DeviceConnected;
            device.Connect();
            device.AutoSubmitReport = false;
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~VigemMapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Temporary event handler for logging the user index of a ViGEm device.
        /// </summary>
        private void DeviceConnected(object sender, Xbox360FeedbackReceivedEventArgs args)
        {
            // Device has connected
            deviceConnected = true;

            // Log the user index
            Console.WriteLine($"Created new ViGEmBus device with user index {args.LedNumber}");

            // Unregister the event handler
            (sender as IXbox360Controller).FeedbackReceived -= DeviceConnected;
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        public XboxResult HandlePacket(CommandId command, ReadOnlySpan<byte> data)
        {
            if (device == null)
                throw new ObjectDisposedException(nameof(device));

            if (!deviceConnected)
                return XboxResult.Pending;

            switch (command)
            {
                case CommandId.Keystroke:
                    return HandleKeystroke(data);

                default:
                    return OnPacketReceived(command, data);
            }
        }

        protected abstract XboxResult OnPacketReceived(CommandId command, ReadOnlySpan<byte> data);

        private unsafe XboxResult HandleKeystroke(ReadOnlySpan<byte> data)
        {
            if (!MapGuideButton)
                return XboxResult.Success;

            if (data.Length < sizeof(Keystroke))
                return XboxResult.InvalidMessage;

            // Multiple keystrokes can be sent in a single message
            var keys = MemoryMarshal.Cast<byte, Keystroke>(data);
            foreach (var key in keys)
            {
                if ((KeyCode)key.Keycode == KeyCode.LeftWindows)
                {
                    device.SetButtonState(Xbox360Button.Guide, key.Pressed);
                    device.SubmitReport();
                }
            }

            return XboxResult.Success;
        }

        /// <summary>
        /// Performs cleanup for the object.
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
                // Reset report
                try
                {
                    device?.ResetReport(); 
                    device?.SubmitReport();
                }
                catch
                { }

                // Disconnect device
                try { device?.Disconnect(); } catch {}
                device = null;
            }
        }
    }
}