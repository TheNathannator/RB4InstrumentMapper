using System;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// Maps GHL guitar inputs to a ViGEmBus device.
    /// </summary>
    internal class GHLGuitarViGEmMapper : ViGEmMapper
    {
        private XboxGHLGuitarKeepAlive keepAlive;

        public GHLGuitarViGEmMapper(IBackendClient client)
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

            HandleReport(device, guitarReport);
            return SubmitReport();
        }

        private XboxGHLGuitarPlayerLeds GetPlayerLeds()
        {
            switch (userIndex)
            {
                case 1: return XboxGHLGuitarPlayerLeds.Player1;
                case 2: return XboxGHLGuitarPlayerLeds.Player2;
                case 3: return XboxGHLGuitarPlayerLeds.Player3;
                case 4: return XboxGHLGuitarPlayerLeds.Player4;

                default:
                    return XboxGHLGuitarPlayerLeds.All;
            }
        }

        /// <summary>
        /// Maps guitar input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in XboxGHLGuitarInput report)
        {
            // Menu buttons
            device.SetButtonState(Xbox360Button.Start, report.Pause);
            device.SetButtonState(Xbox360Button.Back, report.HeroPower);
            device.SetButtonState(Xbox360Button.LeftThumb, report.GHTV);

            // Dpad/strum
            device.SetButtonState(Xbox360Button.Up, report.DpadUp | report.StrumUp);
            device.SetButtonState(Xbox360Button.Down, report.DpadDown | report.StrumDown);
            device.SetButtonState(Xbox360Button.Left, report.DpadLeft);
            device.SetButtonState(Xbox360Button.Right, report.DpadRight);

            short strum = report.StrumUp ? short.MaxValue
                : report.StrumDown ? short.MinValue
                : (short)0;
            device.SetAxisValue(Xbox360Axis.LeftThumbY, strum);

            // Frets
            device.SetButtonState(Xbox360Button.A, report.Black1);
            device.SetButtonState(Xbox360Button.B, report.Black2);
            device.SetButtonState(Xbox360Button.Y, report.Black3);
            device.SetButtonState(Xbox360Button.X, report.White1);
            device.SetButtonState(Xbox360Button.LeftShoulder, report.White2);
            device.SetButtonState(Xbox360Button.RightShoulder, report.White3);

            // Whammy
            // Swapped compared to other guitars; also rests at center instead of negative end
            device.SetAxisValue(Xbox360Axis.RightThumbY, report.WhammyBar.ScaleToInt16());
            // Tilt
            // Swapped compared to other guitars
            device.SetAxisValue(Xbox360Axis.RightThumbX, report.Tilt.ScaleToInt16());
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            keepAlive?.Dispose();
            keepAlive = null;
        }
    }
}
