using System;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Parsing
{
    class VjoyMapper : IDeviceMapper
    {
        private vJoy.JoystickState state = new vJoy.JoystickState();
        private uint deviceId = 0;

        /// <summary>
        /// Creates a new VjoyMapper.
        /// </summary>
        public VjoyMapper()
        {
            deviceId = VjoyStatic.ClaimNextAvailableDevice();
            state.bDevice = (byte)deviceId;
            Console.WriteLine($"Acquired vJoy device with ID of {deviceId}");
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~VjoyMapper()
        {
            Close();
        }

        /// <summary>
        /// Parses an input report.
        /// </summary>
        public void ParseInput(ReadOnlySpan<byte> data, byte length)
        {
            // Reset report
            state.ResetState();

            // Parse the respective device
            switch (length)
            {
                case Length.Input_Guitar:
                    ParseGuitar(data);
                    break;

                case Length.Input_Drums:
                    ParseDrums(data);
                    break;

                default:
                    // Don't parse unknown input reports
                    return;
            }

            // Send data
            VjoyStatic.Client.UpdateVJD(deviceId, ref state);
        }

        /// <summary>
        /// Parses common button data from an input report.
        /// </summary>
        private void ParseCoreButtons(ushort buttons)
        {
            // Menu
            if ((buttons & GamepadButton.Menu) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Fifteen;
            }

            // Options
            if ((buttons & GamepadButton.Options) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Sixteen;
            }

            // D-pad to POV
            if ((buttons & GamepadButton.DpadUp) != 0)
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.UpLeft;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    state.bHats = VjoyStatic.PoV.UpRight;
                }
                else
                {
                    state.bHats = VjoyStatic.PoV.Up;
                }
            }
            else if ((buttons & GamepadButton.DpadDown) != 0)
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.DownLeft;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    state.bHats = VjoyStatic.PoV.DownRight;
                }
                else
                {
                    state.bHats = VjoyStatic.PoV.Down;
                }
            }
            else
            {
                if ((buttons & GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.Left;
                }
                else if ((buttons & GamepadButton.DpadRight) != 0)
                {
                    state.bHats = VjoyStatic.PoV.Right;
                }
                else
                {
                    state.bHats = VjoyStatic.PoV.Neutral;
                }
            }

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
            // The fret data aligns with how we want it to be set in the vJoy device, so it can be mapped directly
            state.Buttons |= data[GuitarOffset.UpperFrets];
            // Lower frets are mapped on top of the upper frets to allow both sets to be used in-game
            state.Buttons |= data[GuitarOffset.LowerFrets];

            // Whammy
            // Value ranges from 0 (not pressed) to 255 (fully pressed)
            state.AxisY = data[GuitarOffset.WhammyBar].ScaleToInt32();

            // Tilt
            // Value ranges from 0 to 255
            // It seems to have a threshold of around 0x70 though,
            // after a certain point values will get floored to 0
            state.AxisZ = data[GuitarOffset.Tilt].ScaleToInt32();

            // Pickup switch
            // Reported values are 0x00, 0x10, 0x20, 0x30, and 0x40 (ranges from 0 to 64)
            state.AxisX = data[GuitarOffset.PickupSwitch].ScaleToInt32();
        }

        /// <summary>
        /// Parses drums input data from an input report.
        /// </summary>
        private void ParseDrums(ReadOnlySpan<byte> data)
        {
            // Buttons
            ParseCoreButtons(data.GetUInt16BE(DrumOffset.Buttons));

            // Pads
            // Red pad
            if ((data[DrumOffset.PadVels] & DrumPadVel.Red) != 0)
            {
                state.Buttons |= VjoyStatic.Button.One;
            }

            // Yellow pad
            if ((data[DrumOffset.PadVels] & DrumPadVel.Yellow) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Two;
            }

            // Blue pad
            if ((data[DrumOffset.PadVels] & DrumPadVel.Blue) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Three;
            }

            // Green pad
            if ((data[DrumOffset.PadVels] & DrumPadVel.Green) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Four;
            }


            // Cymbals
            // Yellow cymbal
            if ((data[DrumOffset.CymbalVels] & DrumCymVel.Yellow) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Six;
            }

            // Blue cymbal
            if ((data[DrumOffset.CymbalVels] & DrumCymVel.Blue) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Seven;
            }

            // Green cymbal
            if ((data[DrumOffset.CymbalVels] & DrumCymVel.Green) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Eight;
            }


            // Kick pedals
            // Kick 1
            if ((data[DrumOffset.Buttons] & DrumButton.KickOne) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Five;
            }

            // Kick 2
            if ((data[DrumOffset.Buttons] & DrumButton.KickTwo) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Nine;
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
                // Don't reset the state to preserve other button information
                // state.ResetState();

                state.Buttons |= (data[KeycodeOffset.PressedState] != 0) ? VjoyStatic.Button.Fourteen : 0;
                VjoyStatic.Client.UpdateVJD(deviceId, ref state);
            }
        }

        /// <summary>
        /// Performs cleanup for the vJoy mapper.
        /// </summary>
        public void Close()
        {
            VjoyStatic.ReleaseDevice(deviceId);
        }
    }
}
