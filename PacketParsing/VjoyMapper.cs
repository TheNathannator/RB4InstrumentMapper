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
                    break;
            }

            // Send data
            VjoyStatic.Client.UpdateVJD(deviceId, ref state);

            if (PacketParser.PacketDebug)
            {
                string debugData = $", Input: {data.ToHexString()}";
                Console.WriteLine(debugData);
                Logging.Packet_WriteLine(debugData);
            }
        }

        /// <summary>
        /// Parses common button data from an input report.
        /// </summary>
        private void ParseCoreButtons(ushort buttons)
        {
            if (PacketParser.PacketDebug)
            {
                string debugData = $"Buttons: {buttons.ToString("X4")}";
                Console.Write(debugData);
                Logging.Packet_Write(debugData);
            }

            // Menu
            if ((buttons | GamepadButton.Menu) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Fifteen;
            }

            // Options
            if ((buttons | GamepadButton.Options) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Sixteen;
            }

            // D-pad to POV
            if ((buttons | GamepadButton.DpadUp) != 0)
            {
                if ((buttons | GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.UpLeft;
                }
                else if ((buttons | GamepadButton.DpadRight) != 0)
                {
                    state.bHats = VjoyStatic.PoV.UpRight;
                }
                else
                {
                    state.bHats = VjoyStatic.PoV.Up;
                }
            }
            else if ((buttons | GamepadButton.DpadDown) != 0)
            {
                if ((buttons | GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.DownLeft;
                }
                else if ((buttons | GamepadButton.DpadRight) != 0)
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
                if ((buttons | GamepadButton.DpadLeft) != 0)
                {
                    state.bHats = VjoyStatic.PoV.Left;
                }
                else if ((buttons | GamepadButton.DpadRight) != 0)
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

            // Left stick
            state.AxisX = data.GetInt16LE(GamepadOffset.LeftStickX);
            state.AxisY = data.GetInt16LE(GamepadOffset.LeftStickY);

            // Don't map anything else, as there are not enough axes and this is meant for debug purposes only
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
            // Lower frets are mapped on top of the upper frets to allow both sets to be used in-game
            frets |= data[GuitarOffset.LowerFrets];

            // The fret data aligns with how we want it to be set in the vJoy device, so it can be mapped directly
            state.Buttons |= frets;

            // Axes
            int whammy = data[GuitarOffset.WhammyBar].ScaleToInt32();
            int tilt = data[GuitarOffset.Tilt].ScaleToInt32();
            int pickup = data[GuitarOffset.PickupSwitch].ScaleToInt32();

            // Whammy
            // Value ranges from 0 (not pressed) to 255 (fully pressed)
            state.AxisY = whammy;

            // Tilt
            // Value ranges from 0 to 255
            // It seems to have a threshold of around 0x70 though,
            // after a certain point values will get floored to 0
            state.AxisZ = tilt;

            // Pickup switch
            // Reported values are 0x00, 0x10, 0x20, 0x30, and 0x40 (ranges from 0 to 64)
            state.AxisX = pickup;

            if (PacketParser.PacketDebug)
            {
                string debugData = $", Frets: {frets.ToString("X2")}, Whammy: {whammy.ToString("X8")}, Tilt: {tilt.ToString("X8")}, Pickup Switch: {pickup.ToString("X8")}";
                Console.WriteLine(debugData);
                Logging.Packet_WriteLine(debugData);
            }
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

            // Red pad
            if (redPad != 0)
            {
                state.Buttons |= VjoyStatic.Button.One;
            }

            // Yellow pad
            if (yellowPad != 0)
            {
                state.Buttons |= VjoyStatic.Button.Two;
            }

            // Blue pad
            if (bluePad != 0)
            {
                state.Buttons |= VjoyStatic.Button.Three;
            }

            // Green pad
            if (greenPad != 0)
            {
                state.Buttons |= VjoyStatic.Button.Four;
            }


            // Yellow cymbal
            if (yellowCym != 0)
            {
                state.Buttons |= VjoyStatic.Button.Six;
            }

            // Blue cymbal
            if (blueCym != 0)
            {
                state.Buttons |= VjoyStatic.Button.Seven;
            }

            // Green cymbal
            if (greenCym != 0)
            {
                state.Buttons |= VjoyStatic.Button.Eight;
            }


            // Kick pedals
            // Kick 1
            if ((data[DrumOffset.Buttons] | DrumButton.KickOne) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Five;
            }

            // Kick 2
            if ((data[DrumOffset.Buttons] | DrumButton.KickTwo) != 0)
            {
                state.Buttons |= VjoyStatic.Button.Nine;
            }

            if (PacketParser.PacketDebug)
            {
                string debugData = $", Pads: Red: {redPad.ToString("X2")}, Yellow: {yellowPad.ToString("X2")}, Blue: {bluePad.ToString("X2")}, Green: {greenPad.ToString("X2")}; Cymbals: Yellow: {yellowCym.ToString("X2")}, Blue: {blueCym.ToString("X2")}, Green: {greenCym.ToString("X2")}";
                Console.WriteLine(debugData);
                Logging.Packet_WriteLine(debugData);
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

            if (PacketParser.PacketDebug)
            {
                string debugData = $", Virtual key: {data.ToHexString()}";
                Console.WriteLine(debugData);
                Logging.Packet_WriteLine(debugData);
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
