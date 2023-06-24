using System;
using System.Collections.Generic;
using RB4InstrumentMapper.Vigem;
using RB4InstrumentMapper.Vjoy;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Factory for device mappers.
    /// </summary>
    internal static class MapperFactory
    {
        // Device interface GUIDs to check when getting the device mapper
        private static readonly Dictionary<Guid, Func<MappingMode, IDeviceMapper>> guidToMapper = new Dictionary<Guid, Func<MappingMode, IDeviceMapper>>()
        {
            { DeviceGuids.MadCatzGuitar, GetGuitarMapper },
            { DeviceGuids.PdpGuitar, GetGuitarMapper },
            { DeviceGuids.MadCatzDrumkit, GetDrumsMapper },
            { DeviceGuids.PdpDrumkit, GetDrumsMapper },
#if DEBUG
            { DeviceGuids.XboxGamepad, GetGamepadMapper },
#endif
        };

        public static IDeviceMapper GetMapper(IReadOnlyList<Guid> interfaceGuids, MappingMode mode)
        {
            // Get unique interface GUID
            Guid interfaceGuid = default;
            foreach (var guid in interfaceGuids)
            {
                if (!guidToMapper.ContainsKey(guid))
                    continue;

                if (interfaceGuid != default)
                {
                    Console.WriteLine($"More than one unique interface GUID found! Cannot get specific mapper, using fallback mapper instead.");
                    Console.WriteLine($"Consider filing a GitHub issue with the GUIDs below so that this can be addressed:");
                    foreach (var guid2 in interfaceGuids)
                    {
                        Console.WriteLine($"- {guid2}");
                    }
                    return GetFallbackMapper(mode);
                }

                interfaceGuid = guid;
            }

            if (interfaceGuid == default)
            {
                Console.WriteLine($"Could not find interface GUID for device! Using fallback mapper instead.");
                Console.WriteLine($"Consider filing a GitHub issue with the GUIDs below so that this can be addressed:");
                foreach (var guid2 in interfaceGuids)
                {
                    Console.WriteLine($"- {guid2}");
                }
                return GetFallbackMapper(mode);
            }

            // Get mapper creation delegate for interface GUID
            if (!guidToMapper.TryGetValue(interfaceGuid, out var func))
            {
                Console.WriteLine($"Could not get a specific mapper for interface GUID {interfaceGuid}! Using fallback mapper instead.");
                Console.WriteLine($"Consider filing a GitHub issue with the GUID above so that it can be assigned the correct mapper.");
                return GetFallbackMapper(mode);
            }

            return func(mode);
        }

#if DEBUG
        private static IDeviceMapper GetGamepadMapper(MappingMode mode)
            => GetMapper<GamepadVigemMapper, GamepadVjoyMapper>(mode, $"Created new {mode} gamepad mapper");
#endif

        private static IDeviceMapper GetGuitarMapper(MappingMode mode)
            => GetMapper<GuitarVigemMapper, GuitarVjoyMapper>(mode, $"Created new {mode} guitar mapper");

        private static IDeviceMapper GetDrumsMapper(MappingMode mode)
            => GetMapper<DrumsVigemMapper, DrumsVjoyMapper>(mode, $"Created new {mode} drumkit mapper");

        public static IDeviceMapper GetFallbackMapper(MappingMode mode)
            => GetMapper<FallbackVigemMapper, FallbackVjoyMapper>(mode, $"Created new fallback {mode} mapper");

        private static IDeviceMapper GetMapper<TVigem, TVjoy>(MappingMode mode, string creationMessage)
            where TVigem : class, IDeviceMapper, new()
            where TVjoy : class, IDeviceMapper, new()
        {
            try
            {
                IDeviceMapper mapper;
                switch (mode)
                {
                    case MappingMode.ViGEmBus:
                        mapper = VigemClient.AreDevicesAvailable ? new TVigem() : null;
                        break;
                    case MappingMode.vJoy:
                        mapper = VjoyClient.AreDevicesAvailable ? new TVjoy() : null;
                        // Check if all devices have been used
                        if (mapper != null && !VjoyClient.AreDevicesAvailable)
                            Console.WriteLine("vJoy device limit reached, no new devices will be handled.");
                        break;
                    default: throw new NotImplementedException($"Unhandled mapping mode {mode}!");
                }

                if (mapper != null)
                    Console.WriteLine(creationMessage);
                return mapper;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create mapper for device: {ex.GetFirstLine()}");
                return null;
            }
        }
    }
}