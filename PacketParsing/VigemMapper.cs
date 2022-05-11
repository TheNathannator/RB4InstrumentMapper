using System;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace RB4InstrumentMapper.Parsing
{
    class VigemMapper : IDeviceMapper
    {
        /// <summary>
        /// The device to map to.
        /// </summary>
        private IXbox360Controller device;

        /// <summary>
        /// Whether or not feedback has been received to indicate that the device has connected.
        /// </summary>
        private bool deviceConnected = false;

        /// <summary>
        /// Creates a new VigemMapper.
        /// </summary>
        public VigemMapper()
        {
            device = VigemStatic.CreateDevice();
            device.FeedbackReceived += ReceiveUserIndex;

            try
            {
                device.Connect();
            }
            catch (VigemNoFreeSlotException ex)
            {
                device = null;
                throw new ParseException("ViGEmBus device slots are full.", ex);
            }

            device.AutoSubmitReport = false;
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~VigemMapper()
        {
            Close();
        }

        /// <summary>
        /// Temporary event handler for logging the user index of a ViGEm device.
        /// </summary>
        void ReceiveUserIndex(object sender, Xbox360FeedbackReceivedEventArgs args)
        {
            // Device has connected
            deviceConnected = true;

            // Log the user index
            Console.WriteLine($"Created new ViGEmBus device with user index {args.LedNumber}");

            // Unregister the event handler
            (sender as IXbox360Controller).FeedbackReceived -= ReceiveUserIndex;
        }

        /// <summary>
        /// Parses an input report.
        /// </summary>
        public void ParseInput(ReadOnlySpan<byte> data, byte length)
        {
            if (!deviceConnected)
            {
                // Device has not connected yet
                return;
            }

            // Reset report
            device.ResetReport();

            switch (length)
            {
                case Length.Input_Guitar:
                    ParseGuitar(data);
                    break;

                case Length.Input_Drums:
                    ParseDrums(data);
                    break;
                
                default:
                    // Don't parse unknown button data
                    return;
            }

            // Send data
            device.SubmitReport();
        }

        /// <summary>
        /// Parses common button data from an input report.
        /// </summary>
        private void ParseCoreButtons(ushort buttons)
        {
            // Menu
            device.SetButtonState(Xbox360Button.Start, (buttons & GamepadButton.Menu) != 0);
            // Options
            device.SetButtonState(Xbox360Button.Back, (buttons & GamepadButton.Options) != 0);

            // Dpad
            device.SetButtonState(Xbox360Button.Up, (buttons & GamepadButton.DpadUp) != 0);
            device.SetButtonState(Xbox360Button.Down, (buttons & GamepadButton.DpadDown) != 0);
            device.SetButtonState(Xbox360Button.Left, (buttons & GamepadButton.DpadLeft) != 0);
            device.SetButtonState(Xbox360Button.Right, (buttons & GamepadButton.DpadRight) != 0);

            // Other buttons are not mapped here since they may have specific uses
        }

        /// <summary>
        /// Parses guitar input data from an input report.
        /// </summary>
        private void ParseGuitar(ReadOnlySpan<byte> data)
        {
            // Buttons
            ParseCoreButtons(data.GetUInt16BE(GuitarOffset.Buttons));

            // Frets
            byte frets = data[GuitarOffset.UpperFrets];
            frets |= data[GuitarOffset.LowerFrets];

            device.SetButtonState(Xbox360Button.A, (frets & GuitarFret.Green) != 0);
            device.SetButtonState(Xbox360Button.B, (frets & GuitarFret.Red) != 0);
            device.SetButtonState(Xbox360Button.Y, (frets & GuitarFret.Yellow) != 0);
            device.SetButtonState(Xbox360Button.X, (frets & GuitarFret.Blue) != 0);
            device.SetButtonState(Xbox360Button.LeftShoulder, (frets & GuitarFret.Orange) != 0);

            // Whammy
            device.SetAxisValue(Xbox360Axis.RightThumbX, data[GuitarOffset.WhammyBar].ScaleToInt16());
            // Tilt
            device.SetAxisValue(Xbox360Axis.RightThumbY, data[GuitarOffset.Tilt].ScaleToInt16());
            // Pickup Switch
            device.SetSliderValue(Xbox360Slider.LeftTrigger, data[GuitarOffset.PickupSwitch]);
        }

        /// <summary>
        /// Parses drums input data from an input report.
        /// </summary>
        private void ParseDrums(ReadOnlySpan<byte> data)
        {
            // Buttons
            ParseCoreButtons(data.GetUInt16BE(DrumOffset.Buttons));

            // Pads and cymbals
            byte redPad    = (byte)(data[DrumOffset.PadVels] >> 4);
            byte yellowPad = (byte)(data[DrumOffset.PadVels] & DrumPadVel.Yellow);
            byte bluePad   = (byte)(data[DrumOffset.PadVels + 1] >> 4);
            byte greenPad  = (byte)(data[DrumOffset.PadVels + 1] & DrumPadVel.Green);

            byte yellowCym = (byte)(data[DrumOffset.CymbalVels] >> 4);
            byte blueCym   = (byte)(data[DrumOffset.CymbalVels] & DrumPadVel.Blue);
            byte greenCym  = (byte)(data[DrumOffset.CymbalVels + 1] >> 4);

            // Color flags
            device.SetButtonState(Xbox360Button.B, (redPad) != 0);
            device.SetButtonState(Xbox360Button.Y, (yellowPad | yellowCym) != 0);
            device.SetButtonState(Xbox360Button.X, (bluePad | blueCym) != 0);
            device.SetButtonState(Xbox360Button.A, (greenPad | greenCym) != 0);

            // Pad flag
            device.SetButtonState(Xbox360Button.RightThumb,
                (redPad | yellowPad | bluePad | greenPad) != 0);
            // Cymbal flag
            device.SetButtonState(Xbox360Button.RightShoulder,
                (yellowCym | blueCym | greenCym) != 0);

            // Velocities
            // device.SetAxisValue(
            //     Xbox360Axis.LeftThumbX,
            //     ByteToVelocity(redPad)
            // );
            // device.SetAxisValue(
            //     Xbox360Axis.LeftThumbY,
            //     ByteToVelocityNegative((byte)(yellowPad | yellowCym))
            // );
            // device.SetAxisValue(
            //     Xbox360Axis.RightThumbX,
            //     ByteToVelocity((byte)(bluePad | blueCym))
            // );
            // device.SetAxisValue(
            //     Xbox360Axis.RightThumbY,
            //     ByteToVelocityNegative((byte)(greenPad | greenCym))
            // );

            /// <summary>
            /// Scales a byte to a drums velocity value.
            /// </summary>
            // short ByteToVelocity(byte value)
            // {
            //     // TODO: Figure out if this is necessary
            //     // Currently, this assumes the max from the kit is 0x04
            //     value = (byte)(value * 0x40 - 1);

            //     return (short)(
            //         (~value.ScaleToUInt16()) >> 1
            //     );
            // }

            /// <summary>
            /// Scales a byte to a negative drums velocity value.
            /// </summary>
            // short ByteToVelocityNegative(byte value)
            // {
            //     // TODO: Figure out if this is necessary
            //     // Currently, this assumes the max from the kit is 0x04
            //     value = (byte)(value * 0x40 - 1);

            //     return (short)(
            //         ((~value.ScaleToUInt16()) >> 1) | 0x8000
            //     );
            // }
        }

        /// <summary>
        /// Parses a virtual key report.
        /// </summary>
        public void ParseVirtualKey(ReadOnlySpan<byte> data, byte length)
        {
            // Only respond to the Left Windows keycode, as this is what the guide button reports.
            if (data[KeycodeOffset.Keycode] == Keycodes.LeftWin)
            {
                // Don't reset the report to preserve other button information
                // device.ResetReport();
                device.SetButtonState(Xbox360Button.Guide, data[KeycodeOffset.PressedState] != 0);
                device.SubmitReport();
            }
        }

        /// <summary>
        /// Performs cleanup for the object.
        /// </summary>
        public void Close()
        {
            try { device?.Disconnect(); } catch {}
            device = null;
        }
    }
}
