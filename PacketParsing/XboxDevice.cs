using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Interface for Xbox devices.
    /// </summary>
    class XboxDevice
    {
        /// <summary>
        /// Mapper interface to use.
        /// </summary>
        private IDeviceMapper deviceMapper;

        // Lock off parameterless constructor
        private XboxDevice() {}

        /// <summary>
        /// Creates a new XboxDevice with the given device ID and parsing mode.
        /// </summary>
        public XboxDevice(ParsingMode parseMode)
        {
            switch (parseMode)
            {
                case ParsingMode.ViGEmBus:
                    deviceMapper = new VigemMapper();
                    break;

                case ParsingMode.vJoy:
                    deviceMapper = new VjoyMapper();
                    break;
            }
        }

        /// <summary>
        /// Performs cleanup on object finalization.
        /// </summary>
        ~XboxDevice()
        {
            Close();
        }

        /// <summary>
        /// Parses command data from a packet.
        /// </summary>
        public void ParseCommand(ReadOnlySpan<byte> commandData)
        {
            switch (commandData[CommandOffset.CommandId])
            {
                case CommandId.Input:
                    deviceMapper.ParseInput(commandData.Slice(Length.CommandHeader), commandData[CommandOffset.DataLength], commandData[CommandOffset.SequenceCount]);
                    break;

                // Probably don't actually want to parse the guide button and output it to the device,
                // so as to not interfere with Windows processes that use it
                // case CommandId.VirtualKey:
                //     deviceMapper.ParseVirtualKey(commandData.Slice(Length.CommandHeader), commandData[CommandOffset.DataLength], commandData[CommandOffset.SequenceCount]);
                //     break;

                default:
                    // Don't do anything with unrecognized command IDs
                    break;
            }
        }

        /// <summary>
        /// Performs cleanup for the device.
        /// </summary>
        public void Close()
        {
            deviceMapper?.Close();
            deviceMapper = null;
        }
    }
}