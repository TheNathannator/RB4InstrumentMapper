using System;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

#if DEBUG

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps gamepad inputs to a ViGEmBus device.
    /// </summary>
    internal class GamepadVigemMapper : VigemMapper
    {
        public GamepadVigemMapper() : base()
        {
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        protected override XboxResult OnPacketReceived(CommandId command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case CommandId.Input:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        /// <summary>
        /// Parses an input report.
        /// </summary>
        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(GamepadInput) || !MemoryMarshal.TryRead(data, out GamepadInput gamepadReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, gamepadReport);

            // Send data
            device.SubmitReport();
            return XboxResult.Success;
        }

        /// <summary>
        /// Maps gamepad input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in GamepadInput report)
        {
            // Face buttons
            device.SetButtonState(Xbox360Button.A, report.A);
            device.SetButtonState(Xbox360Button.B, report.B);
            device.SetButtonState(Xbox360Button.X, report.X);
            device.SetButtonState(Xbox360Button.Y, report.Y);

            // Dpad
            device.SetButtonState(Xbox360Button.Up, report.DpadUp);
            device.SetButtonState(Xbox360Button.Down, report.DpadDown);
            device.SetButtonState(Xbox360Button.Left, report.DpadLeft);
            device.SetButtonState(Xbox360Button.Right, report.DpadRight);

            // Dpad
            device.SetButtonState(Xbox360Button.LeftShoulder, report.LeftBumper);
            device.SetButtonState(Xbox360Button.RightShoulder, report.RightBumper);
            device.SetButtonState(Xbox360Button.LeftThumb, report.LeftStickPress);
            device.SetButtonState(Xbox360Button.RightThumb, report.RightStickPress);

            // Menu and Options
            device.SetButtonState(Xbox360Button.Start, report.Menu);
            device.SetButtonState(Xbox360Button.Back, report.Options);

            // Sticks
            device.SetAxisValue(Xbox360Axis.LeftThumbX, report.LeftStickX);
            device.SetAxisValue(Xbox360Axis.LeftThumbY, report.LeftStickY);
            device.SetAxisValue(Xbox360Axis.RightThumbX, report.RightStickX);
            device.SetAxisValue(Xbox360Axis.RightThumbY, report.RightStickY);

            // Triggers
            device.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(report.LeftTrigger >> 2));
            device.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(report.RightTrigger >> 2));
        }
    }
}

#endif