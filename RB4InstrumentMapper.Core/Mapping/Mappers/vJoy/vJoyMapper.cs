using vJoyInterfaceWrap;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// A mapper that maps to a vJoy device.
    /// </summary>
    internal abstract class vJoyMapper : DeviceMapper
    {
        protected vJoy.JoystickState state = new vJoy.JoystickState();
        protected uint deviceId = 0;

        public vJoyMapper(IBackendClient client)
            : base(client)
        {
            deviceId = vJoyInstance.GetNextAvailableID();
            if (deviceId == 0)
            {
                throw new vJoyException("No vJoy devices are available.");
            }

            if (!vJoyInstance.AcquireDevice(deviceId))
            {
                throw new vJoyException($"Could not claim vJoy device {deviceId}.");
            }

            state.bDevice = (byte)deviceId;
            Logging.WriteLineVerbose($"Acquired vJoy device with ID of {deviceId}");
        }

        protected override void MapGuideButton(bool pressed)
        {
            state.SetButton(vJoyButton.Fourteen, pressed);
            vJoyInstance.UpdateDevice(deviceId, ref state);
        }

        // vJoy axes range from 0x0000 to 0x8000, but are exposed as full ints for some reason
        protected static void SetAxis(ref int axisField, byte value)
        {
            axisField = (value * 0x0101) >> 1;
        }

        protected static void SetAxis(ref int axisField, short value)
        {
            axisField = ((ushort)value ^ 0x8000) >> 1;
        }

        protected static void SetAxisInverted(ref int axisField, short value)
        {
            axisField = 0x8000 - (((ushort)value ^ 0x8000) >> 1);
        }

        /// <summary>
        /// Parses the state of the d-pad.
        /// </summary>
        protected static void ParseDpad(ref vJoy.JoystickState state, XboxGamepadButton buttons)
        {
            vJoyHat direction;
            if ((buttons & XboxGamepadButton.DpadUp) != 0)
            {
                if ((buttons & XboxGamepadButton.DpadLeft) != 0)
                {
                    direction = vJoyHat.UpLeft;
                }
                else if ((buttons & XboxGamepadButton.DpadRight) != 0)
                {
                    direction = vJoyHat.UpRight;
                }
                else
                {
                    direction = vJoyHat.Up;
                }
            }
            else if ((buttons & XboxGamepadButton.DpadDown) != 0)
            {
                if ((buttons & XboxGamepadButton.DpadLeft) != 0)
                {
                    direction = vJoyHat.DownLeft;
                }
                else if ((buttons & XboxGamepadButton.DpadRight) != 0)
                {
                    direction = vJoyHat.DownRight;
                }
                else
                {
                    direction = vJoyHat.Down;
                }
            }
            else
            {
                if ((buttons & XboxGamepadButton.DpadLeft) != 0)
                {
                    direction = vJoyHat.Left;
                }
                else if ((buttons & XboxGamepadButton.DpadRight) != 0)
                {
                    direction = vJoyHat.Right;
                }
                else
                {
                    direction = vJoyHat.Neutral;
                }
            }

            state.bHats = (uint)direction;
        }

        protected XboxResult SubmitReport()
        {
            vJoyInstance.UpdateDevice(deviceId, ref state);
            return XboxResult.Success;
        }

        public override void ResetReport()
        {
            state.Reset();
            vJoyInstance.UpdateDevice(deviceId, ref state);
        }

        protected override void DisposeUnmanagedResources()
        {
            // Free device
            ResetReport();
            vJoyInstance.ReleaseDevice(deviceId);
            deviceId = 0;
        }
    }
}
