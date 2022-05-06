using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Static vJoy client.
    /// </summary>
    static class VjoyStatic
    {
        /// <summary>
        /// Static vJoy client.
        /// </summary>
        private static vJoy client = new vJoy();

        /// <summary>
        /// Gets the vJoy client.
        /// </summary>
        public static vJoy Client
        {
            get => client;
        }

        /// <summary>
        /// Gets the next available device ID.
        /// </summary>
        public static uint GetNextAvailableID()
        {
            // Get available devices
            for (uint deviceId = 1; deviceId <= 16; deviceId++)
            {
                // Ensure device is available
                if (client.GetVJDStatus(deviceId) == VjdStat.VJD_STAT_FREE)
                {
                    // Check that the vJoy device is configured correctly
                    int numButtons = client.GetVJDButtonNumber(deviceId);
                    int numContPov = client.GetVJDContPovNumber(deviceId);
                    bool xExists = client.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_X); // X axis
                    bool yExists = client.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_Y); // Y axis
                    bool zExists = client.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_Z); // Z axis

                    if (numButtons >= 16 &&
                        numContPov >= 1 &&
                        xExists &&
                        yExists &&
                        zExists
                    )
                    {
                        return deviceId;
                    }
                }
            }

            // No devices available
            return 0;
        }

        /// <summary>
        /// Claims a vJoy device.
        /// </summary>
        public static uint ClaimNextAvailableDevice()
        {
            uint deviceId = GetNextAvailableID();

            if (deviceId == 0)
            {
                throw new ParseException("No new vJoy devices are available.");
            }

            if (!client.AcquireVJD(deviceId))
            {
                throw new ParseException($"Could not claim vJoy device {deviceId}.");
            }

            return deviceId;
        }

        /// <summary>
        /// Releases a vJoy device.
        /// </summary>
        public static void ReleaseDevice(uint deviceId)
        {
            // Ensure device is owned
            if (client.GetVJDStatus(deviceId) == VjdStat.VJD_STAT_OWN)
            {
                client.RelinquishVJD(deviceId);
            }
        }

        public static void FreeAllDevices()
        {
            for (uint i = 1; i <= 16; i++)
            {
                ReleaseDevice(i);
            }
        }

        /// <summary>
        /// Resets the values of this state.
        /// </summary>
        public static void ResetState(this vJoy.JoystickState state)
        {
            // Only these values are used, don't reset anything else to save on performance
            state.Buttons = Button.None;
            state.bHats = PoV.Neutral;
            state.AxisX = 0;
            state.AxisY = 0;
            state.AxisZ = 0;
        }

        /// <summary>
        /// vJoy button flag constants.
        /// </summary>
        public class Button
        {
            public const uint
            None = 0,
            One = 1 << 0,
            Two = 1 << 1,
            Three = 1 << 2,
            Four = 1 << 3,
            Five = 1 << 4,
            Six = 1 << 5,
            Seven = 1 << 6,
            Eight = 1 << 7,
            Nine = 1 << 8,
            Ten = 1 << 9,
            Eleven = 1 << 10,
            Twelve = 1 << 11,
            Thirteen = 1 << 12,
            Fourteen = 1 << 13,
            Fifteen = 1 << 14,
            Sixteen = 1 << 15;
        }

        /// <summary>
        /// vJoy PoV hat constants.
        /// </summary>
        public class PoV
        {
            // vJoy continuous PoV hat values range from 0 to 35999 (measured in 1/100 of a degree).
            // The value is measured clockwise, with up being 0.
            public const uint
            Neutral = 0xFFFFFFFF,
            Up = 0,
            UpRight = 4500,
            Right = 9000,
            DownRight = 13500,
            Down = 18000,
            DownLeft = 22500,
            Left = 27000,
            UpLeft = 31500;
        }
    }
}
