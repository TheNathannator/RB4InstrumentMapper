using System;
using System.Runtime.CompilerServices;
using vJoyInterfaceWrap;

using Button = RB4InstrumentMapper.Parsing.VjoyStatic.Button;

namespace RB4InstrumentMapper.Parsing
{
    class VjoyMapper : IDeviceMapper
    {
        private vJoy.JoystickState state = new vJoy.JoystickState();
        private uint deviceId = 0;

        private int prevInputSeqCount = -1;
        private int prevVirtualKeySeqCount = -1;

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
        public void ParseInput(ReadOnlySpan<byte> data, byte length, byte sequenceCount)
        {
            // Don't parse the same report twice
            if (sequenceCount == prevInputSeqCount)
            {
                return;
            }
            else
            {
                prevInputSeqCount = sequenceCount;
            }

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
        /// Sets the state of the specified button.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetButton(uint button, bool condition)
        {
            if (condition)
            {
                state.Buttons |= button;
            }
            else
            {
                state.Buttons &= ~button;
            }
        }

        /// <summary>
        /// Parses common button data from an input report.
        /// </summary>
        private void ParseCoreButtons(ushort buttons)
        {
            // Menu
            SetButton(Button.Fifteen, (buttons & GamepadButton.Menu) != 0);

            // Options
            SetButton(Button.Sixteen, (buttons & GamepadButton.Options) != 0);

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
            byte frets = data[GuitarOffset.UpperFrets];
            frets |= data[GuitarOffset.LowerFrets];

            SetButton(Button.One, (frets & GuitarFret.Green) != 0);
            SetButton(Button.Two, (frets & GuitarFret.Red) != 0);
            SetButton(Button.Three, (frets & GuitarFret.Yellow) != 0);
            SetButton(Button.Four, (frets & GuitarFret.Blue) != 0);
            SetButton(Button.Five, (frets & GuitarFret.Orange) != 0);

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
            SetButton(Button.One, (data[DrumOffset.PadVels] & DrumPadVel.Red) != 0);
            SetButton(Button.Two, (data[DrumOffset.PadVels] & DrumPadVel.Yellow) != 0);
            SetButton(Button.Three, (data[DrumOffset.PadVels] & DrumPadVel.Blue) != 0);
            SetButton(Button.Four, (data[DrumOffset.PadVels] & DrumPadVel.Green) != 0);

            // Cymbals
            SetButton(Button.Six, (data[DrumOffset.CymbalVels] & DrumCymVel.Yellow) != 0);
            SetButton(Button.Seven, (data[DrumOffset.CymbalVels] & DrumCymVel.Blue) != 0);
            SetButton(Button.Eight, (data[DrumOffset.CymbalVels] & DrumCymVel.Green) != 0);

            // Kick pedals
            SetButton(Button.Five, (data[DrumOffset.Buttons] & DrumButton.KickOne) != 0);
            SetButton(Button.Nine, (data[DrumOffset.Buttons] & DrumButton.KickTwo) != 0);
        }

        /// <summary>
        /// Parses a virtual key report.
        /// </summary>
        public void ParseVirtualKey(ReadOnlySpan<byte> data, byte length, byte sequenceCount)
        {
            // Don't parse the same report twice
            if (sequenceCount == prevVirtualKeySeqCount)
            {
                return;
            }
            else
            {
                prevVirtualKeySeqCount = sequenceCount;
            }

            // Only respond to the Left Windows keycode, as this is what the guide button reports.
            if (data[KeycodeOffset.Keycode] == Keycodes.LeftWin)
            {
                SetButton(Button.Fourteen, data[KeycodeOffset.PressedState] != 0);
                VjoyStatic.Client.UpdateVJD(deviceId, ref state);
            }
        }

        /// <summary>
        /// Performs cleanup for the vJoy mapper.
        /// </summary>
        public void Close()
        {
            // Reset report
            state.ResetState();
            VjoyStatic.Client.UpdateVJD(deviceId, ref state);

            // Free device
            VjoyStatic.ReleaseDevice(deviceId);
        }
    }
}
