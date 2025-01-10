using System;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Parsing;

namespace RB4InstrumentMapper.Mapping
{
    /// <summary>
    /// Maps guitar inputs to a ViGEmBus device, with RPCS3 compatibilty mappings.
    /// </summary>
    internal class GuitarRPCS3Mapper : ViGEmMapper
    {
        public GuitarRPCS3Mapper(IBackendClient client)
            : base(client)
        {
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxGuitarInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxGuitarInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, guitarReport);

            // Send data
            return SubmitReport();
        }

        /// <summary>
        /// Maps guitar input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in XboxGuitarInput report)
        {
            // Changes from the normal ViGEm mapping are based on the PS3 guitar report
            // https://github.com/TheNathannator/PlasticBand/blob/main/Docs/Instruments/5-Fret%20Guitar/Rock%20Band/PS3%20and%20Wii.md
            // RPCS3 by default maps XInput gamepads 1:1 with PS3 ones,
            // so we change the bindings here to make any rebinding unnecessary

            // Menu and Options
            var buttons = (XboxGamepadButton)report.Buttons;
            device.SetButtonState(Xbox360Button.Start, (buttons & XboxGamepadButton.Menu) != 0);
            device.SetButtonState(Xbox360Button.Back, (buttons & XboxGamepadButton.Options) != 0);

            // Dpad
            device.SetButtonState(Xbox360Button.Up, (buttons & XboxGamepadButton.DpadUp) != 0);
            device.SetButtonState(Xbox360Button.Down, (buttons & XboxGamepadButton.DpadDown) != 0);
            device.SetButtonState(Xbox360Button.Left, (buttons & XboxGamepadButton.DpadLeft) != 0);
            device.SetButtonState(Xbox360Button.Right, (buttons & XboxGamepadButton.DpadRight) != 0);

            // Frets
            device.SetButtonState(Xbox360Button.A, report.Green);
            device.SetButtonState(Xbox360Button.B, report.Red);
            device.SetButtonState(Xbox360Button.Y, report.Yellow);
            device.SetButtonState(Xbox360Button.X, report.Blue);
            device.SetButtonState(Xbox360Button.LeftShoulder, report.Orange);

            // Lower fret flag
            // This uses the L2 button bit on PS3 guitars, which we can't set directly,
            // so we set the trigger axis instead
            device.SetSliderValue(Xbox360Slider.LeftTrigger, report.LowerFretsPressed ? byte.MaxValue : byte.MinValue);

            // Whammy
            device.SetAxisValue(Xbox360Axis.RightThumbX, report.WhammyBar.ScaleToInt16());
            // Tilt
            // Button instead of an axis
            // TODO: The threshold here should probably be configurable/calibratable
            device.SetButtonState(Xbox360Button.RightShoulder, report.Tilt >= 0xD0);
            // Pickup Switch
            // Right stick Y instead of left trigger
            device.SetAxisValue(Xbox360Axis.RightThumbY,
                GuitarViGEmMapper.CalculatePickupSwitch(report.PickupSwitch).ScaleToInt16());
        }
    }
}
