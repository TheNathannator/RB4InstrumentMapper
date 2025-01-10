using System;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Parsing;

namespace RB4InstrumentMapper.Mapping
{
    /// <summary>
    /// Maps Riffmaster guitar inputs to a ViGEmBus device.
    /// </summary>
    internal class RiffmasterViGEmMapper : ViGEmMapper
    {
        public RiffmasterViGEmMapper(IBackendClient client)
            : base(client)
        {
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxRiffmasterInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxRiffmasterInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, guitarReport);

            // Send data
            return SubmitReport();
        }

        /// <summary>
        /// Maps guitar input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in XboxRiffmasterInput report)
        {
            // Guitar inputs
            GuitarViGEmMapper.HandleReport(device, report.Base);

            // Joystick
            device.SetAxisValue(Xbox360Axis.LeftThumbX, report.JoystickX);
            device.SetAxisValue(Xbox360Axis.LeftThumbY, report.JoystickY);
            device.SetButtonState(Xbox360Button.LeftThumb, report.JoystickClick | report.Base.LowerFretsPressed);
        }
    }
}
