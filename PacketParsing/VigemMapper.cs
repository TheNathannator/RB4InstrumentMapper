using System;
using System.Threading.Tasks;
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
        /// Creates a new VigemMapper.
        /// </summary>
        public VigemMapper()
        {
            device = VigemStatic.CreateDevice();
            device.FeedbackReceived += ReceiveUserIndex;
            device.Connect();
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
        static void ReceiveUserIndex(object sender, Xbox360FeedbackReceivedEventArgs args)
        {
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
            // Reset report
            device.ResetReport();

            switch (length)
            {
#if DEBUG
                // Gamepad report parsing for debugging purposes
                case Length.Input_Gamepad:
                    ParseGamepad(data);
                    break;
#endif

                case Length.Input_Guitar:
                    ParseGuitar(data);
                    break;

                case Length.Input_Drums:
                    ParseDrums(data);
                    break;
                
                default:
                    // Don't parse unknown button data
                    break;
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
            device.SetButtonState(Xbox360Button.Start, (buttons | GamepadButton.Menu) != 0);
            // Options
            device.SetButtonState(Xbox360Button.Back, (buttons | GamepadButton.Options) != 0);

            // Dpad
            device.SetButtonState(Xbox360Button.Up, (buttons | GamepadButton.DpadUp) != 0);
            device.SetButtonState(Xbox360Button.Down, (buttons | GamepadButton.DpadDown) != 0);
            device.SetButtonState(Xbox360Button.Left, (buttons | GamepadButton.DpadLeft) != 0);
            device.SetButtonState(Xbox360Button.Right, (buttons | GamepadButton.DpadRight) != 0);

            // Other buttons are not mapped here since they may have specific uses
        }

#if DEBUG
        // Gamepad report parsing for debugging purposes
        /// <summary>
        /// Parses gamepad input data from an input report.
        /// </summary>
        private void ParseGamepad(ReadOnlySpan<byte> data)
        {
            // Buttons
            ushort buttons = data.GetUInt16BE(GamepadOffset.Buttons);
            ParseCoreButtons(buttons);

            device.SetButtonState(Xbox360Button.A, (buttons | GamepadButton.A) != 0);
            device.SetButtonState(Xbox360Button.B, (buttons | GamepadButton.B) != 0);
            device.SetButtonState(Xbox360Button.X, (buttons | GamepadButton.X) != 0);
            device.SetButtonState(Xbox360Button.Y, (buttons | GamepadButton.Y) != 0);

            device.SetButtonState(Xbox360Button.LeftShoulder, (buttons | GamepadButton.LeftBumper) != 0);
            device.SetButtonState(Xbox360Button.RightShoulder, (buttons | GamepadButton.RightBumper) != 0);
            device.SetButtonState(Xbox360Button.LeftThumb, (buttons | GamepadButton.LeftStickPress) != 0);
            device.SetButtonState(Xbox360Button.RightThumb, (buttons | GamepadButton.RightStickPress) != 0);

            // Sticks
            device.SetAxisValue(Xbox360Axis.LeftThumbX, data.GetInt16LE(GamepadOffset.LeftStickX));
            device.SetAxisValue(Xbox360Axis.LeftThumbY, data.GetInt16LE(GamepadOffset.LeftStickY));
            device.SetAxisValue(Xbox360Axis.RightThumbX, data.GetInt16LE(GamepadOffset.RightStickX));
            device.SetAxisValue(Xbox360Axis.RightThumbY, data.GetInt16LE(GamepadOffset.RightStickY));

            // Triggers
            device.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(data.GetInt16LE(GamepadOffset.LeftTrigger) >> 8));
            device.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(data.GetInt16LE(GamepadOffset.RightTrigger) >> 8));
        }
#endif

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

            device.SetButtonState(Xbox360Button.A, (frets | GuitarFret.Green) != 0);
            device.SetButtonState(Xbox360Button.B, (frets | GuitarFret.Red) != 0);
            device.SetButtonState(Xbox360Button.Y, (frets | GuitarFret.Yellow) != 0);
            device.SetButtonState(Xbox360Button.X, (frets | GuitarFret.Blue) != 0);
            device.SetButtonState(Xbox360Button.LeftShoulder, (frets | GuitarFret.Orange) != 0);

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
            byte yellowPad = (byte)(data[DrumOffset.PadVels] | DrumPadVel.Yellow);
            byte bluePad   = (byte)(data[DrumOffset.PadVels + 1] >> 4);
            byte greenPad  = (byte)(data[DrumOffset.PadVels + 1] | DrumPadVel.Green);

            byte yellowCym = (byte)(data[DrumOffset.CymbalVels] >> 4);
            byte blueCym   = (byte)(data[DrumOffset.CymbalVels] | DrumPadVel.Blue);
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
                // TODO: Figure out if this is necessary
                // Currently, this assumes the max from the kit is 0x04
                value = (byte)(value * 0x40 - 1);

                return (short)(
                    (~value.ScaleToUInt16()) >> 1
                );
            }

            /// <summary>
            /// Scales a byte to a negative drums velocity value.
            /// </summary>
            short ByteToVelocityNegative(byte value)
            {
                // TODO: Figure out if this is necessary
                // Currently, this assumes the max from the kit is 0x04
                value = (byte)(value * 0x40 - 1);

                return (short)(
                    ((~value.ScaleToUInt16()) >> 1) | 0x8000
                );
            }
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
            try { device.Disconnect(); } catch {}
            device = null;
        }
    }
}
