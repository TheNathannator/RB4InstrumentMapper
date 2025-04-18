using System;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// The RPCS3 mapper used when device type could not be determined. Maps based on report length.
    /// </summary>
    internal class FallbackRPCS3Mapper : ViGEmMapper
    {
        public FallbackRPCS3Mapper(IBackendClient client)
            : base(client)
        {
        }

        protected override unsafe XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxGuitarInput.CommandId:
                // These have the same value
                // case XboxDrumInput.CommandId:
                // #if ENABLE_GAMEPAD_MAPPING
                // case XboxGamepadInput.CommandId:
                // #endif
                    return ParseInput(data);

                case XboxGHLGuitarInput.CommandId:
                    // Deliberately limit to the exact size
                    if (data.Length != sizeof(XboxGHLGuitarInput) || !ParsingUtils.TryRead(data, out XboxGHLGuitarInput guitarReport))
                        return XboxResult.InvalidMessage;

                    // No mapping differences between RPCS3 and ViGEm modes
                    GHLGuitarViGEmMapper.HandleReport(device, guitarReport);
                    return SubmitReport();

                default:
                    return XboxResult.Success;
            }
        }

        // The previous state of the yellow/blue cymbals
        private int previousDpadCymbals;
        // The current state of the d-pad mask from the hit yellow/blue cymbals
        private int dpadMask;

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (data.Length == sizeof(XboxGuitarInput) && ParsingUtils.TryRead(data, out XboxGuitarInput guitarReport))
            {
                GuitarRPCS3Mapper.HandleReport(device, guitarReport);
            }
            else if (data.Length == sizeof(XboxDrumInput) && ParsingUtils.TryRead(data, out XboxDrumInput drumReport))
            {
                DrumsRPCS3Mapper.HandleReport(device, drumReport, ref previousDpadCymbals, ref dpadMask);
            }
#if ENABLE_GAMEPAD_MAPPING
            else if (data.Length == sizeof(XboxGamepadInput) && ParsingUtils.TryRead(data, out XboxGamepadInput gamepadReport))
            {
                GamepadViGEmMapper.HandleReport(device, gamepadReport);
            }
#endif
            else
            {
                // Not handled
                return XboxResult.Success;
            }

            return SubmitReport();
        }
    }
}
