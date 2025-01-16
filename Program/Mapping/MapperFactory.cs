using System;
using System.Collections.Generic;
using RB4InstrumentMapper.Parsing;

namespace RB4InstrumentMapper.Mapping
{
    public enum MappingMode
    {
        NotSet = 0,
        ViGEmBus = 1,
        vJoy = 2,
        RPCS3 = 3,
        shadPS4 = 4,
    }

    /// <summary>
    /// Creates a device mapper for a client.
    /// </summary>
    internal static class MapperFactory
    {
        private delegate DeviceMapper CreateMapper(IBackendClient client);

        /// <summary>
        /// The controller emulator to use.
        /// </summary>
        public static MappingMode MapperMode { get; set; } = MappingMode.NotSet;

        private static readonly Dictionary<(ushort vendorId, ushort productId), CreateMapper> hardwareIdLookup
            = new Dictionary<(ushort, ushort), CreateMapper>()
        {
            // Guitars
            { (0x0738, 0x4161), GetGuitarMapper }, // MadCatz Stratocaster
            { (0x0E6F, 0x0170), GetGuitarMapper }, // PDP Jaguar
            { (0x0E6F, 0x0248), GetGuitarMapper }, // PDP Riffmaster

            // Drumkits
            { (0x0738, 0x4262), GetDrumsMapper }, // MadCatz
            { (0x0E6F, 0x0171), GetDrumsMapper }, // PDP

            // Other
            { (0x1430, 0x079B), GetGHLGuitarMapper }, // Guitar Hero Live guitar
            { (0x0738, 0x4164), GetWirelessLegacyMapper }, // MadCatz Wireless Legacy adapter

            // Gamepads
            { (0x045E, 0x02DD), GetGamepadMapper }, // Microsoft 1st-revision gamepad
            { (0x045E, 0x0B00), GetGamepadMapper }, // Microsoft Elite Series 2 gamepad
        };

        private static readonly Dictionary<Guid, CreateMapper> interfaceGuidLookup = new Dictionary<Guid, CreateMapper>()
        {
            { XboxDeviceGuids.MadCatzGuitar,  GetGuitarMapper },
            { XboxDeviceGuids.PdpGuitar,      GetGuitarMapper },

            { XboxDeviceGuids.MadCatzDrumkit, GetDrumsMapper },
            { XboxDeviceGuids.PdpDrumkit,     GetDrumsMapper },
    
            { XboxDeviceGuids.ActivisionGuitarHeroLive, GetGHLGuitarMapper },

            { XboxDeviceGuids.MadCatzLegacyWireless, GetWirelessLegacyMapper },

            { XboxDeviceGuids.XboxGamepad,    GetGamepadMapper },
        };

        // Interface GUIDs to ignore when more than one supported interface is found
        private static readonly HashSet<Guid> conflictIgnoredIds = new HashSet<Guid>()
        {
            // GHL guitars list both a unique interface and the gamepad interface
            XboxDeviceGuids.XboxGamepad,
        };

        private static CreateMapper GetByInterfaceIds(HashSet<Guid> interfaceGuids)
        {
            // Get unique interface GUID
            Guid interfaceGuid = default;
            foreach (var guid in interfaceGuids)
            {
                if (!interfaceGuidLookup.ContainsKey(guid))
                    continue;

                if (interfaceGuid != default)
                {
                    // Ignore IDs known to have conflicts
                    if (conflictIgnoredIds.Contains(guid))
                        continue;

                    Logging.WriteLine($"More than one recognized interface found! Cannot get specific mapper, device will not be mapped.");
                    Logging.WriteLine($"Consider filing a GitHub issue with the GUIDs below if this device should be supported:");
                    foreach (var guid2 in interfaceGuids)
                    {
                        Logging.WriteLine($"- {guid2}");
                    }
                    return null;
                }

                interfaceGuid = guid;
            }

            if (interfaceGuid == default)
            {
                Logging.WriteLine($"Could not find any supported interface IDs! Device will not be mapped.");
                Logging.WriteLine($"Consider filing a GitHub issue with the GUIDs below if this device should be supported:");
                foreach (var guid2 in interfaceGuids)
                {
                    Logging.WriteLine($"- {guid2}");
                }
                return null;
            }

            // Get mapper creation delegate for interface GUID
            if (!interfaceGuidLookup.TryGetValue(interfaceGuid, out var func))
            {
                Logging.WriteLine($"Could not get a specific mapper for interface {interfaceGuid}! Device will not be mapped.");
                Logging.WriteLine($"Consider filing a GitHub issue with the GUID above if this device should be supported.");
                return null;
            }

            return func;
        }

        public static DeviceMapper GetByHardwareIds(IBackendClient client)
        {
            if (!hardwareIdLookup.TryGetValue((client.VendorId, client.ProductId), out var func))
            {
                // Verbose since hardware ID lookup is meant for GameInput,
                // and we don't want to warn unnecessarily for devices that don't need to be handled
                Logging.WriteLineVerbose($"Device with hardware IDs {client.VendorId:X4}:{client.ProductId:X4} is not recognized! Device will not be mapped.");
                return new DummyMapper(client);
            }

            try
            {
                return func(client);
            }
            catch (Exception ex)
            {
                Logging.WriteException("Failed to create mapper for device!", ex);
                return null;
            }
        }

        public static DeviceMapper GetByInterfaceIds(IBackendClient client, HashSet<Guid> interfaceGuids)
        {
            var func = GetByInterfaceIds(interfaceGuids);
            if (func == null)
                return new DummyMapper(client);

            try
            {
                return func(client);
            }
            catch (Exception ex)
            {
                Logging.WriteException("Failed to create mapper for device!", ex);
                return null;
            }
        }

        private static DeviceMapper GetMapper(IBackendClient client, CreateMapper createViGEm, CreateMapper createvJoy,
            CreateMapper createRpcs3, CreateMapper createshadPS4)
        {
            DeviceMapper mapper;
            bool devicesAvailable;

            var mode = MapperMode;
            switch (mode)
            {
                case MappingMode.ViGEmBus:
                    mapper = ViGEmInstance.AreDevicesAvailable ? createViGEm(client) : null;
                    devicesAvailable = ViGEmInstance.AreDevicesAvailable;
                    break;

                case MappingMode.RPCS3:
                    mapper = ViGEmInstance.AreDevicesAvailable ? createRpcs3(client) : null;
                    devicesAvailable = ViGEmInstance.AreDevicesAvailable;
                    break;
                
                case MappingMode.shadPS4:
                    mapper = ViGEmInstance.AreDevicesAvailable ? createshadPS4(client) : null;
                    devicesAvailable = ViGEmInstance.AreDevicesAvailable;
                    break;

                case MappingMode.vJoy:
                    mapper = vJoyInstance.AreDevicesAvailable ? createvJoy(client) : null;
                    devicesAvailable = vJoyInstance.AreDevicesAvailable;
                    break;

                default:
                    throw new NotImplementedException($"Unhandled mapping mode {mode}!");
            }

            if (mapper != null)
            {
                Logging.WriteLine($"Created new {mapper.GetType().Name}");
                if (!devicesAvailable)
                    Logging.WriteLine("Device limit reached, no new devices will be handled.");
            }

            return mapper;
        }

        public static DeviceMapper GetGamepadMapper(IBackendClient client)
        {
#if DEBUG
            Logging.WriteLine("Warning: Gamepads are only supported in debug mode for testing purposes, they will not work in release builds.");
            return GetMapper(client,
                (c) => new GamepadViGEmMapper(c),
                (c) => new GamepadvJoyMapper(c),
                // No RPCS3 or shadPS4 mapper, as this is for testing only
                (c) => new GamepadViGEmMapper(c),
                (c) => new GamepadViGEmMapper(c)
            );
#else
            return null;
#endif
        }

        public static DeviceMapper GetGuitarMapper(IBackendClient client)
        {
            const ushort RIFFMASTER_VENDOR_ID = 0x0E6F;
            const ushort RIFFMASTER_PRODUCT_ID = 0x0248;

            bool isRiffmaster = client.VendorId == RIFFMASTER_VENDOR_ID &&
                client.ProductId == RIFFMASTER_PRODUCT_ID;

            CreateMapper createViGEm;
            if (isRiffmaster)
                createViGEm = (c) => new RiffmasterViGEmMapper(c);
            else
                createViGEm = (c) => new GuitarViGEmMapper(c);
            
            // I (aoiyu_) am not 100% sure if the base guitars share the same problems as the Riffmaster.
            // Sorry if they do, I just don't have a non-Riffmaster to test on.
            CreateMapper createshadPS4;
            if (isRiffmaster)
                createshadPS4 = (c) => new RiffmastershadPS4Mapper(c);
            else
                createshadPS4 = (c) => new GuitarViGEmMapper(c);

            return GetMapper(client,
                createViGEm,
                (c) => new GuitarvJoyMapper(c),
                (c) => new GuitarRPCS3Mapper(c),
                createshadPS4
            );
        }

        public static DeviceMapper GetDrumsMapper(IBackendClient client) => GetMapper(client,
            (c) => new DrumsViGEmMapper(c),
            (c) => new DrumsvJoyMapper(c),
            (c) => new DrumsRPCS3Mapper(c),
            // No mapping differences that I (aoiyu_) know of.
            (c) => new DrumsViGEmMapper(c)
        );

        public static DeviceMapper GetGHLGuitarMapper(IBackendClient client) => GetMapper(client,
            (c) => new GHLGuitarViGEmMapper(c),
            (c) => new GHLGuitarvJoyMapper(c),
            // No mapping differences between RPCS3 and ViGEm modes
            (c) => new GHLGuitarViGEmMapper(c),
            // No mapping differences that I (aoiyu_) know of.
            (c) => new GHLGuitarViGEmMapper(c)
        );

        public static DeviceMapper GetWirelessLegacyMapper(IBackendClient client)
        {
            var mapper = new WirelessLegacyMapper(client);
            Logging.WriteLine($"Created new {nameof(WirelessLegacyMapper)} mapper");
            return mapper;
        }

        public static DeviceMapper GetFallbackMapper(IBackendClient client) => GetMapper(client,
            (c) => new FallbackViGEmMapper(c),
            (c) => new FallbackvJoyMapper(c),
            (c) => new FallbackRPCS3Mapper(c),
            // No mapping differences that I (aoiyu_) know of.
            (c) => new FallbackViGEmMapper(c)
        );
    }
}