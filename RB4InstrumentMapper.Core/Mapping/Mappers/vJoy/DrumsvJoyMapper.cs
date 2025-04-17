using System;
using RB4InstrumentMapper.Core.Parsing;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// Maps drumkit inputs to a vJoy device.
    /// </summary>
    internal class DrumsvJoyMapper : vJoyMapper
    {
        public DrumsvJoyMapper(IBackendClient client)
            : base(client)
        {
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxDrumInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxDrumInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(ref state, guitarReport);
            return SubmitReport();
        }

        /// <summary>
        /// Maps drumkit input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, XboxDrumInput report)
        {
            // Menu and Options
            var buttons = (XboxGamepadButton)report.Buttons;
            state.SetButton(vJoyButton.Fifteen, (buttons & XboxGamepadButton.Menu) != 0);
            state.SetButton(vJoyButton.Sixteen, (buttons & XboxGamepadButton.Options) != 0);

            // D-pad
            ParseDpad(ref state, buttons);

            // Face buttons
            state.SetButton(vJoyButton.Four, (buttons & XboxGamepadButton.A) != 0);
            state.SetButton(vJoyButton.One, (buttons & XboxGamepadButton.B) != 0);
            state.SetButton(vJoyButton.Three, (buttons & XboxGamepadButton.X) != 0);
            state.SetButton(vJoyButton.Two, (buttons & XboxGamepadButton.Y) != 0);

            // Pads
            state.SetButton(vJoyButton.One, report.RedPad != 0);
            state.SetButton(vJoyButton.Two, report.YellowPad != 0);
            state.SetButton(vJoyButton.Three, report.BluePad != 0);
            state.SetButton(vJoyButton.Four, report.GreenPad != 0);

            // Cymbals
            state.SetButton(vJoyButton.Six, report.YellowCymbal != 0);
            state.SetButton(vJoyButton.Seven, report.BlueCymbal != 0);
            state.SetButton(vJoyButton.Eight, report.GreenCymbal != 0);

            // Kick pedals
            state.SetButton(vJoyButton.Five, (report.Buttons & (ushort)XboxDrumButton.KickOne) != 0);
            state.SetButton(vJoyButton.Nine, (report.Buttons & (ushort)XboxDrumButton.KickTwo) != 0);
        }
    }
}
