using System;
using RB4InstrumentMapper.Core.Parsing;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// Maps GHL guitar inputs to a vJoy device.
    /// </summary>
    internal class GHLGuitarvJoyMapper : vJoyMapper
    {
        private XboxGHLGuitarKeepAlive keepAlive;

        public GHLGuitarvJoyMapper(IBackendClient client)
            : base(client)
        {
            keepAlive = new XboxGHLGuitarKeepAlive(client);

            // Set player LED
            var setLeds = XboxGHLGuitarSetPlayerLeds.Create(GetPlayerLeds());
            client.SendMessage(setLeds);
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxGHLGuitarInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxGHLGuitarInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(ref state, guitarReport);
            return SubmitReport();
        }

        private XboxGHLGuitarPlayerLeds GetPlayerLeds()
        {
            switch (deviceId)
            {
                case 1:  return XboxGHLGuitarPlayerLeds.Player1;
                case 2:  return XboxGHLGuitarPlayerLeds.Player2;
                case 3:  return XboxGHLGuitarPlayerLeds.Player3;
                case 4:  return XboxGHLGuitarPlayerLeds.Player4;
                case 5:  return XboxGHLGuitarPlayerLeds.Player5;
                case 6:  return XboxGHLGuitarPlayerLeds.Player6;
                case 7:  return XboxGHLGuitarPlayerLeds.Player7;
                case 8:  return XboxGHLGuitarPlayerLeds.Player8;
                case 9:  return XboxGHLGuitarPlayerLeds.Player9;
                case 10: return XboxGHLGuitarPlayerLeds.Player10;
                case 11: return XboxGHLGuitarPlayerLeds.Player11;
                case 12: return XboxGHLGuitarPlayerLeds.Player12;
                case 13: return XboxGHLGuitarPlayerLeds.Player13;
                case 14: return XboxGHLGuitarPlayerLeds.Player14;
                case 15: return XboxGHLGuitarPlayerLeds.Player15;

                // If someone connects this many devices it's their problem lol
                case 16:
                default:
                    return XboxGHLGuitarPlayerLeds.All;
            }
        }

        /// <summary>
        /// Maps GHL guitar input data to a vJoy device.
        /// </summary>
        internal static void HandleReport(ref vJoy.JoystickState state, XboxGHLGuitarInput report)
        {
            // Menu buttons
            state.SetButton(vJoyButton.Fifteen, report.Pause);
            state.SetButton(vJoyButton.Sixteen, report.HeroPower);
            state.SetButton(vJoyButton.Twelve, report.GHTV);

            // D-pad/strum
            // It would be more efficient to directly map the GHL value to the vJoy value,
            // but that doesn't account for the strum bar being on its own axis
            XboxGamepadButton dpad = 0;
            if (report.DpadUp | report.StrumUp) dpad |= XboxGamepadButton.DpadUp;
            if (report.DpadDown | report.StrumDown) dpad |= XboxGamepadButton.DpadDown;
            if (report.DpadLeft) dpad |= XboxGamepadButton.DpadLeft;
            if (report.DpadRight) dpad |= XboxGamepadButton.DpadRight;
            ParseDpad(ref state, dpad);

            // Frets
            state.SetButton(vJoyButton.One,   report.Black1);
            state.SetButton(vJoyButton.Two,   report.Black2);
            state.SetButton(vJoyButton.Three, report.Black3);
            state.SetButton(vJoyButton.Four,  report.White1);
            state.SetButton(vJoyButton.Five,  report.White2);
            state.SetButton(vJoyButton.Six,   report.White3);

            // Whammy
            // Value ranges from 128 (not pressed) to 255 (fully pressed)
            byte whammy = (byte)((report.WhammyBar - 0x80) * 2);
            SetAxis(ref state.AxisY, whammy);

            // Tilt
            // Value ranges from 0 to 255
            SetAxis(ref state.AxisZ, report.Tilt);
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            keepAlive?.Dispose();
            keepAlive = null;
        }
    }
}
