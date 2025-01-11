using System;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Parsing;

namespace RB4InstrumentMapper.Mapping
{
    /// <summary>
    /// Maps drumkit inputs to a ViGEmBus device, with RPCS3 compatibilty mappings.
    /// </summary>
    internal class DrumsRPCS3Mapper : ViGEmMapper
    {
        public DrumsRPCS3Mapper(IBackendClient client)
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

        // The previous state of the yellow/blue cymbals
        private int previousDpadCymbals;
        // The current state of the d-pad mask from the hit yellow/blue cymbals
        private int dpadMask;

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxDrumInput drumReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, drumReport, ref previousDpadCymbals, ref dpadMask);
            return SubmitReport();
        }

        /// <summary>
        /// Maps drumkit input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in XboxDrumInput report, ref int previousDpadCymbals, ref int dpadMask)
        {
            // Changes from the normal ViGEm mapping are based on the PS3 guitar report
            // https://github.com/TheNathannator/PlasticBand/blob/main/Docs/Instruments/4-Lane%20Drums/PS3%20and%20Wii.md
            // RPCS3 by default maps XInput gamepads 1:1 with PS3 ones,
            // so we change the bindings here to make any rebinding unnecessary

            // Menu and Options
            var buttons = (XboxGamepadButton)report.Buttons;
            device.SetButtonState(Xbox360Button.Start, (buttons & XboxGamepadButton.Menu) != 0);
            device.SetButtonState(Xbox360Button.Back, (buttons & XboxGamepadButton.Options) != 0);

            // Dpad
            DrumsViGEmMapper.MapDpad_HardwareAccurate(device, report, ref previousDpadCymbals, ref dpadMask);

            // Pads and cymbals
            byte redPad    = report.RedPad;
            byte yellowPad = report.YellowPad;
            byte bluePad   = report.BluePad;
            byte greenPad  = report.GreenPad;

            byte yellowCym = report.YellowCymbal;
            byte blueCym   = report.BlueCymbal;
            byte greenCym  = report.GreenCymbal;

            // Color flags
            device.SetButtonState(Xbox360Button.B, (redPad != 0) || ((buttons & XboxGamepadButton.B) != 0));
            device.SetButtonState(Xbox360Button.Y, ((yellowPad | yellowCym) != 0) || ((buttons & XboxGamepadButton.Y) != 0));
            device.SetButtonState(Xbox360Button.X, ((bluePad | blueCym) != 0) || ((buttons & XboxGamepadButton.X) != 0));
            device.SetButtonState(Xbox360Button.A, ((greenPad | greenCym) != 0) || ((buttons & XboxGamepadButton.A) != 0));

            // Pad flag
            // Left stick click instead of right stick click
            device.SetButtonState(Xbox360Button.LeftThumb,
                (redPad | yellowPad | bluePad | greenPad) != 0);
            // Cymbal flag
            // Right stick click instead of right bumper
            device.SetButtonState(Xbox360Button.RightThumb,
                (yellowCym | blueCym | greenCym) != 0);

            // Pedals
            device.SetButtonState(Xbox360Button.LeftShoulder,
                (report.Buttons & (ushort)XboxDrumButton.KickOne) != 0);
            // Right bumper instead of left stick click
            device.SetButtonState(Xbox360Button.RightShoulder,
                (report.Buttons & (ushort)XboxDrumButton.KickTwo) != 0);

            // No velocity axes since it's not feasible to map them
        }
    }
}
