using System;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// The ViGEmBus mapper used when device type could not be determined. Maps based on report length.
    /// </summary>
    internal class FallbackVigemMapper : VigemMapper
    {
        public FallbackVigemMapper() : base()
        {
        }

        /// <summary>
        /// Handles an incoming packet.
        /// </summary>
        protected override XboxResult OnPacketReceived(CommandId command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case CommandId.Input:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        // The previous state of the yellow/blue cymbals
        private int previousDpadCymbals;
        // The current state of the d-pad mask from the hit yellow/blue cymbals
        private int dpadMask;

        /// <summary>
        /// Parses an input report.
        /// </summary>
        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length == sizeof(GuitarInput) && MemoryMarshal.TryRead(data, out GuitarInput guitarReport))
            {
                GuitarVigemMapper.HandleReport(device, guitarReport);
            }
            else if (data.Length == sizeof(DrumInput) && MemoryMarshal.TryRead(data, out DrumInput drumReport))
            {
                DrumsVigemMapper.HandleReport(device, drumReport, ref previousDpadCymbals, ref dpadMask);
            }
#if DEBUG
            else if (data.Length == sizeof(GamepadInput) && MemoryMarshal.TryRead(data, out GamepadInput gamepadReport))
            {
                GamepadVigemMapper.HandleReport(device, gamepadReport);
            }
#endif
            else
            {
                // Not handled
                return XboxResult.Success;
            }

            // Send data
            device.SubmitReport();
            return XboxResult.Success;
        }
    }
}
