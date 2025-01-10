using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    internal enum XboxConfigurationCommand : byte
    {
        PowerOn = 0x00,
        Sleep = 0x01,
        PowerOff = 0x04,
        WirelessPairing = 0x06,
        Reset = 0x07,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxConfiguration
    {
        public const byte CommandId = 0x05;

        public static readonly XboxMessage<XboxConfiguration> PowerOnDevice = new XboxMessage<XboxConfiguration>()
        {
            Header = new XboxCommandHeader()
            {
                CommandId = CommandId,
                Flags = XboxCommandFlags.SystemCommand,
            },
            Data = new XboxConfiguration()
            {
                SubCommand = XboxConfigurationCommand.PowerOn,
            }
        };

        public static readonly XboxMessage<XboxConfiguration> PowerOffDevice = new XboxMessage<XboxConfiguration>()
        {
            Header = new XboxCommandHeader()
            {
                CommandId = CommandId,
                Flags = XboxCommandFlags.SystemCommand,
            },
            Data = new XboxConfiguration()
            {
                SubCommand = XboxConfigurationCommand.PowerOff,
            }
        };

        public static readonly XboxMessage<XboxConfiguration> ResetDevice = new XboxMessage<XboxConfiguration>()
        {
            Header = new XboxCommandHeader()
            {
                CommandId = CommandId,
                Flags = XboxCommandFlags.SystemCommand,
            },
            Data = new XboxConfiguration()
            {
                SubCommand = XboxConfigurationCommand.Reset,
            }
        };

        public XboxConfigurationCommand SubCommand;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XboxConfiguration<TSub>
        where TSub : unmanaged
    {
        public XboxConfigurationCommand SubCommand;
        public TSub SubData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct XboxWirelessPairing
    {
        public const XboxConfigurationCommand SubCommand = XboxConfigurationCommand.WirelessPairing;

        private fixed byte pairingAddress[6];
        public ushort countryCode;
        private fixed byte unknown[6];
    }
}
