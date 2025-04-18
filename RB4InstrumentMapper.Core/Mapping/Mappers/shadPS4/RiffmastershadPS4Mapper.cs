using System;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// Maps Riffmaster guitar inputs to a ViGEmBus device with modifications to support shadPS4.
    /// </summary>
    internal class RiffmastershadPS4Mapper : ViGEmMapper
    {
        public RiffmastershadPS4Mapper(IBackendClient client)
            : base(client)
        {
        }

        private static int currentPickup = 0;
        private static bool pickupDown = false;
        
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
            
            // Whammy Bar
            device.SetAxisValue(Xbox360Axis.LeftThumbY, report.Base.WhammyBar.ScaleToPositiveInt16());
            
            // Tilt
            device.SetAxisValue(Xbox360Axis.RightThumbY, (-1 * (int)Math.Round(report.Base.Tilt.ScaleToInt16() * BackendSettings.RiffmasterSensitivity)).ClampToShort());
            
            // Joystick
            device.SetButtonState(Xbox360Button.LeftThumb, report.JoystickClick | report.Base.LowerFretsPressed);
            
            // Pickup
            HandlePickup(report);
            device.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(currentPickup * 51 - 25));
        }

        private static void HandlePickup(XboxRiffmasterInput report)
        {
            bool pickupSetDown = ((XboxGamepadButton)report.Base.Buttons).HasFlag(XboxGamepadButton.LeftStickPress);
            if (pickupSetDown && !pickupDown)
            {
                pickupDown = true;
                currentPickup++;
                if (currentPickup > 4)
                    currentPickup = 0;
            }
            else if (!pickupSetDown && pickupDown)
            {
                pickupDown = false;
            }
        }
    }
}
