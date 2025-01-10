using System.Runtime.CompilerServices;
using vJoyInterfaceWrap;

namespace RB4InstrumentMapper.Mapping
{
    public static class vJoyExtensions
    {
        /// <summary>
        /// Sets the state of the specified button.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetButton(ref this vJoy.JoystickState state, vJoyButton button, bool set)
        {
            if (set)
            {
                state.Buttons |= (uint)button;
            }
            else
            {
                state.Buttons &= (uint)~button;
            }
        }

        /// <summary>
        /// Resets the values of this state.
        /// </summary>
        public static void Reset(ref this vJoy.JoystickState state)
        {
            // Only reset the values we use
            state.Buttons = (uint)vJoyButton.None;
            state.bHats = (uint)vJoyHat.Neutral;
            state.AxisX = 0;
            state.AxisY = 0;
            state.AxisZ = 0;
        }
    }
}