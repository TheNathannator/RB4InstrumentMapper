using System;
using System.Runtime.InteropServices;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps drumkit inputs to a ViGEmBus device.
    /// </summary>
    internal class DrumsVigemMapper : VigemMapper
    {
        public DrumsVigemMapper() : base()
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

        // Constants for the d-pad masks
        private const int yellowBit = 0x01;
        private const int blueBit = 0x02;

        // The previous state of the yellow/blue cymbals
        private int previousDpadCymbals;
        // The current state of the d-pad mask from the hit yellow/blue cymbals
        private int dpadMask;

        /// <summary>
        /// Parses an input report.
        /// </summary>
        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length != sizeof(DrumInput) || !MemoryMarshal.TryRead(data, out DrumInput drumReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, drumReport, ref previousDpadCymbals, ref dpadMask);

            // Send data
            device.SubmitReport();
            return XboxResult.Success;
        }

        /// <summary>
        /// Maps drumkit input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in DrumInput report, ref int previousDpadCymbals, ref int dpadMask)
        {
            // Menu and Options
            var buttons = (GamepadButton)report.Buttons;
            device.SetButtonState(Xbox360Button.Start, (buttons & GamepadButton.Menu) != 0);
            device.SetButtonState(Xbox360Button.Back, (buttons & GamepadButton.Options) != 0);

            // Dpad
            device.SetButtonState(Xbox360Button.Up, (buttons & GamepadButton.DpadUp) != 0);
            device.SetButtonState(Xbox360Button.Down, (buttons & GamepadButton.DpadDown) != 0);
            device.SetButtonState(Xbox360Button.Left, (buttons & GamepadButton.DpadLeft) != 0);
            device.SetButtonState(Xbox360Button.Right, (buttons & GamepadButton.DpadRight) != 0);

            // Pads and cymbals
            byte redPad    = report.RedPad;
            byte yellowPad = report.YellowPad;
            byte bluePad   = report.BluePad;
            byte greenPad  = report.GreenPad;

            byte yellowCym = report.YellowCymbal;
            byte blueCym   = report.BlueCymbal;
            byte greenCym  = report.GreenCymbal;

            // Yellow and blue cymbal trigger d-pad up and down respectively on the RB2/3 kit we're emulating
            // However, they only trigger one or the other, not both at the same time, so we need to mimic that
            int cymbalMask = (yellowCym != 0 ? yellowBit : 0) | (blueCym != 0 ? blueBit : 0);
            if (cymbalMask != previousDpadCymbals)
            {
                if (cymbalMask == 0)
                    dpadMask = 0;

                // This could probably be done more simply, but this works
                if (dpadMask != 0)
                {
                    // D-pad is already set
                    // Only remove the set value
                    if ((cymbalMask & yellowBit) == 0)
                        dpadMask &= ~yellowBit;
                    else if ((cymbalMask & blueBit) == 0)
                        dpadMask &= ~blueBit;
                }

                // Explicitly check this so that if the d-pad is cleared but the other cymbal is still active,
                // it will get set to that cymbal's d-pad
                if (dpadMask == 0)
                {
                    // D-pad is not set
                    // If both cymbals are hit at the same time, yellow takes priority
                    if ((cymbalMask & yellowBit) != 0)
                        dpadMask |= yellowBit;
                    else if ((cymbalMask & blueBit) != 0)
                        dpadMask |= blueBit;
                }

                previousDpadCymbals = cymbalMask;
            }

            device.SetButtonState(Xbox360Button.Up, ((dpadMask & yellowBit) != 0) || ((buttons & GamepadButton.DpadUp) != 0));
            device.SetButtonState(Xbox360Button.Down, ((dpadMask & blueBit) != 0) || ((buttons & GamepadButton.DpadDown) != 0));

            // Color flags
            device.SetButtonState(Xbox360Button.B, (redPad != 0) || ((buttons & GamepadButton.B) != 0));
            device.SetButtonState(Xbox360Button.Y, ((yellowPad | yellowCym) != 0) || ((buttons & GamepadButton.Y) != 0));
            device.SetButtonState(Xbox360Button.X, ((bluePad | blueCym) != 0) || ((buttons & GamepadButton.X) != 0));
            device.SetButtonState(Xbox360Button.A, ((greenPad | greenCym) != 0) || ((buttons & GamepadButton.A) != 0));

            // Pad flag
            device.SetButtonState(Xbox360Button.RightThumb,
                (redPad | yellowPad | bluePad | greenPad) != 0);
            // Cymbal flag
            device.SetButtonState(Xbox360Button.RightShoulder,
                (yellowCym | blueCym | greenCym) != 0);

            // Pedals
            device.SetButtonState(Xbox360Button.LeftShoulder,
                (report.Buttons & (ushort)DrumButton.KickOne) != 0);
            device.SetButtonState(Xbox360Button.LeftThumb,
                (report.Buttons & (ushort)DrumButton.KickTwo) != 0);

            // Velocities
            device.SetAxisValue(
                Xbox360Axis.LeftThumbX,
                ByteToVelocity(redPad)
            );
            device.SetAxisValue(
                Xbox360Axis.LeftThumbY,
                ByteToVelocityNegative((byte)(yellowPad | yellowCym))
            );
            device.SetAxisValue(
                Xbox360Axis.RightThumbX,
                ByteToVelocity((byte)(bluePad | blueCym))
            );
            device.SetAxisValue(
                Xbox360Axis.RightThumbY,
                ByteToVelocityNegative((byte)(greenPad | greenCym))
            );

            /// <summary>
            /// Scales a byte to a drums velocity value.
            /// </summary>
            short ByteToVelocity(byte value)
            {
                // Scale the value to fill the byte
                value = (byte)(value * 0x11);

                return (short)(
                    // Bitwise invert to flip the value, then shift down one to exclude the sign bit
                    (~value.ScaleToUInt16()) >> 1
                );
            }

            /// <summary>
            /// Scales a byte to a negative drums velocity value.
            /// </summary>
            short ByteToVelocityNegative(byte value)
            {
                // Scale the value to fill the byte
                value = (byte)(value * 0x11);

                return (short)(
                    // Bitwise invert to flip the value, then shift down one to exclude the sign bit, then add our own
                    ((~value.ScaleToUInt16()) >> 1) | 0x8000
                );
            }
        }
    }
}
