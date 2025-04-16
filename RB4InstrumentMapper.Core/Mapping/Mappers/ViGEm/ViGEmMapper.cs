using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// A mapper that maps to a ViGEmBus device.
    /// </summary>
    internal abstract class ViGEmMapper : DeviceMapper
    {
        /// <summary>
        /// The device to map to.
        /// </summary>
        protected IXbox360Controller device;

        /// <summary>
        /// Whether or not the emulated Xbox 360 controller has connected fully.
        /// </summary>
        protected bool deviceConnected = false;

        /// <summary>
        /// The LED number for the emulated Xbox 360 controller.
        /// </summary>
        protected byte userIndex;

        public ViGEmMapper(IBackendClient client)
            : base(client)
        {
            device = ViGEmInstance.CreateDevice();
            device.FeedbackReceived += DeviceConnected;
            device.Connect();
            device.AutoSubmitReport = false;
        }

        // Temporary event handler to ensure device connection
        private void DeviceConnected(object sender, Xbox360FeedbackReceivedEventArgs args)
        {
            // Device has connected
            deviceConnected = true;

            // Log the user index
            userIndex = args.LedNumber;
            Logging.WriteLineVerbose($"Created new ViGEmBus device with user index {userIndex}");

            // Unregister the event handler
            (sender as IXbox360Controller).FeedbackReceived -= DeviceConnected;
        }

        protected override void MapGuideButton(bool pressed)
        {
            device.SetButtonState(Xbox360Button.Guide, pressed);
            device.SubmitReport();
        }

        protected XboxResult SubmitReport()
        {
            if (!deviceConnected)
                return XboxResult.Pending;

            device.SubmitReport();
            return XboxResult.Success;
        }

        public override void ResetReport()
        {
            try
            {
                device.ResetReport(); 
                device.SubmitReport();
            }
            catch { }
        }

        protected override void DisposeManagedResources()
        {
            if (device != null)
            {
                // Reset report
                ResetReport();

                // Disconnect device
                try
                {
                    device.Disconnect();
                }
                catch { }
            }

            device = null;
        }
    }
}