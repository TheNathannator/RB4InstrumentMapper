using System;
using System.Diagnostics;
using RB4InstrumentMapper.Vjoy;
using vJoyInterfaceWrap;

#if DEBUG

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps gamepad inputs to a vJoy device.
    /// </summary>
    internal class GamepadVjoyMapper : VjoyMapper
    {
        private bool rumbling;
        private Stopwatch rumbleCooldown = new Stopwatch();

        public GamepadVjoyMapper(IBackendClient client)
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

            HandleReport(ref state, gamepadReport);

            // Rumble, for output testing
            short x = Math.Abs(gamepadReport.LeftStickX);
            short y = Math.Abs(gamepadReport.LeftStickY);
            short max = Math.Max(x, y);
            if (max > (short.MaxValue / 10f))
            {
                if (rumbleCooldown.ElapsedMilliseconds > 50)
                {
                    rumbling = true;
                    byte left = (byte)(x >> 8);
                    byte right = (byte)(y >> 8);
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
        /// Maps gamepad input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, XboxGamepadInput report)
        {
            // Buttons and axes are mapped the same way as they display in joy.cpl when used normally

            // Buttons
            state.SetButton(VjoyButton.One, report.A);
            state.SetButton(VjoyButton.Two, report.B);
            state.SetButton(VjoyButton.Three, report.X);
            state.SetButton(VjoyButton.Four, report.Y);

            state.SetButton(VjoyButton.Five, report.LeftBumper);
            state.SetButton(VjoyButton.Six, report.RightBumper);

            state.SetButton(VjoyButton.Seven, report.Options);
            state.SetButton(VjoyButton.Eight, report.Menu);

            state.SetButton(VjoyButton.Nine, report.LeftStickPress);
            state.SetButton(VjoyButton.Ten, report.RightStickPress);

            // D-pad
            ParseDpad(ref state, (XboxGamepadButton)report.Buttons);

            // Left stick
            SetAxis(ref state.AxisX, report.LeftStickX);
            SetAxisInverted(ref state.AxisY, report.LeftStickY);

            // Triggers
            // These are both combined into a single axis
            int triggerAxis = (report.LeftTrigger - report.RightTrigger) * 0x20;
            SetAxis(ref state.AxisZ, (short)triggerAxis);
        }
    }
}

#endif