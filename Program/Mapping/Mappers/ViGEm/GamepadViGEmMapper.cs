using System;
using System.Diagnostics;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Parsing;

#if DEBUG

namespace RB4InstrumentMapper.Mapping
{
    /// <summary>
    /// Maps gamepad inputs to a ViGEmBus device.
    /// </summary>
    internal class GamepadViGEmMapper : ViGEmMapper
    {
        private bool rumbling;
        private Stopwatch rumbleCooldown = new Stopwatch();

        public GamepadViGEmMapper(IBackendClient client)
            : base(client)
        {
            rumbleCooldown.Start();
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxGamepadInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxGamepadInput gamepadReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, gamepadReport);

            // Rumble, for output testing
            float x = Math.Abs(gamepadReport.LeftStickX / 32768f);
            float y = Math.Abs(gamepadReport.LeftStickY / 32768f);
            float max = Math.Max(x, y);
            if (max > 0.1f)
            {
                if (rumbleCooldown.ElapsedMilliseconds > 50)
                {
                    rumbling = true;
                    byte left = (byte)(x * 255);
                    byte right = (byte)(y * 255);
                    client.SendMessage(XboxGamepadRumble.Create(left, right));
                    rumbleCooldown.Restart();
                }
            }
            else if (rumbling)
            {
                rumbling = false;
                client.SendMessage(XboxGamepadRumble.Create(0, 0));
            }

            return SubmitReport();
        }

        /// <summary>
        /// Maps gamepad input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in XboxGamepadInput report)
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