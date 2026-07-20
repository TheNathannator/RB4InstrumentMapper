using System;
using System.Collections.Generic;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// Maps devices connected to a RB4 wireless legacy adapter to virtual controllers.
    /// </summary>
    internal class WirelessLegacyMapper : DeviceMapper
    {
        private struct SubMapper
        {
            public XboxWirelessLegacyDeviceType DeviceType;
            public DeviceMapper Mapper;
        }

        // Mappers are not guaranteed to be created for each device, unknown subtypes will be ignored and have none
        private readonly Dictionary<byte, SubMapper> mappers = new Dictionary<byte, SubMapper>();

        public WirelessLegacyMapper(IBackendClient client)
            : base(client)
        {
            client.SendMessage(XboxWirelessLegacyRequestDevices.RequestDevices);
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxWirelessLegacyInputHeader.CommandId:
                    return HandleInput(data);

                case XboxWirelessLegacyDeviceConnect.CommandId:
                    return HandleConnection(data);

                case XboxWirelessLegacyDeviceDisconnect.CommandId:
                    return HandleDisconnection(data);
            }

            return XboxResult.Success;
        }

        private unsafe XboxResult HandleInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxWirelessLegacyInputHeader header))
                return XboxResult.InvalidMessage;

            // Find the mapper for the given user index
            byte userIndex = header.UserIndex;
            if (!mappers.TryGetValue(userIndex, out var subMapper))
            {
                // Don't flag as invalid if no devices are currently connected
                // GIP devices are required to send a state report during initialization, so it's
                // possible by design to receive one for index 0 before having a mapper for it
                if (userIndex == 0 && mappers.Count < 1)
                {
                    return XboxResult.Pending;
                }

                Logging.WriteLineVerbose($"Missing mapper for wireless legacy user index {userIndex}!");
                return XboxResult.InvalidMessage;
            }

            // Verify the device type
            if (subMapper.DeviceType != header.DeviceType)
            {
                Logging.WriteLineVerbose($"Wrong input type for wireless legacy user index {userIndex}! Expected {subMapper.DeviceType}, got {header.DeviceType}");
                return XboxResult.InvalidMessage;
            }

            // Slice off the header and pass the rest of the data to the mapper
            // The data afterwards has its own copy of the button mask, so we don't need to do anything else
            data = data.Slice(sizeof(XboxWirelessLegacyInputHeader));
            return subMapper.Mapper?.HandleMessage(XboxWirelessLegacyInputHeader.CommandId, data) ?? XboxResult.Success;
        }

        private XboxResult HandleConnection(ReadOnlySpan<byte> data)
        {
            if (!XboxWirelessLegacyDeviceConnect.TryParse(data, out var connect))
                return XboxResult.InvalidMessage;

            // Clear any existing mapper at this index
            byte userIndex = connect.UserIndex;
            if (mappers.TryGetValue(userIndex, out var subMapper))
            {
                subMapper.Mapper?.Dispose();
            }

            // Initialize with new mapper
            var mapper = GetMapperForDevice(connect);
            if (mapper != null)
            {
                mappers[userIndex] = new SubMapper()
                {
                    DeviceType = connect.DeviceType,
                    Mapper = mapper
                };
            }

            return XboxResult.Success;
        }

        private unsafe XboxResult HandleDisconnection(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxWirelessLegacyDeviceDisconnect disconnect))
                return XboxResult.InvalidMessage;

            // Find the mapper for the given user index
            byte userIndex = disconnect.UserIndex;
            if (!mappers.TryGetValue(userIndex, out var subMapper))
            {
                Logging.WriteLineVerbose($"Missing mapper for legacy adapter user index {userIndex}!");
                return XboxResult.InvalidMessage;
            }

            // Remove the mapper
            subMapper.Mapper?.Dispose();
            mappers.Remove(userIndex);
            return XboxResult.Success;
        }

        public override XboxResult HandleKeystroke(XboxKeystroke key)
        {
            foreach (var subMapper in mappers.Values)
            {
                var result = subMapper.Mapper.HandleKeystroke(key);
                if (result != XboxResult.Success)
                    return result;
            }

            return XboxResult.Success;
        }

        // Handled by the override above
        protected override void MapGuideButton(bool pressed) { }

        public override void ResetReport()
        {
            foreach (var subMapper in mappers.Values)
            {
                subMapper.Mapper.ResetReport();
            }
        }

        private DeviceMapper GetMapperForDevice(in XboxWirelessLegacyDeviceConnect connect)
        {
            var type = connect.DeviceType;
            switch (type)
            {
                case XboxWirelessLegacyDeviceType.Guitar:
                    return MapperFactory.GetGuitarMapper(client);

                case XboxWirelessLegacyDeviceType.Drums:
                    return MapperFactory.GetDrumsMapper(client);

                default:
                    Logging.WriteLine($"User index {connect.UserIndex + 1} on the legacy adapter has an unsupported device type {type} (XInput subtype {connect.XInputSubType})!");
                    Logging.WriteLine("If you think it should be supported, restart capture with packet logging to a file enabled, go through all of the inputs, and create a GitHub issue with the log file attached.");
                    return null;
            }
        }

        protected override void DisposeManagedResources()
        {
            foreach (var subMapper in mappers.Values)
            {
                subMapper.Mapper.Dispose();
            }

            mappers.Clear();
        }
    }
}