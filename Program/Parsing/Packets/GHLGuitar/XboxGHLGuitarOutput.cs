using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace RB4InstrumentMapper.Parsing
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct XboxGHLGuitarOutput
    {
        public const byte CommandId = 0x22;

        public byte SubCommand;
        public fixed byte Data[7];
    }

    [Flags]
    internal enum XboxGHLGuitarPlayerLeds : byte
    {
        None = 0,

        Led1 = 0x01,
        Led2 = 0x02,
        Led3 = 0x04,
        Led4 = 0x08,

        All = Led1 | Led2 | Led3 | Led4,

        Player1 = Led1,
        Player2 = Led2,
        Player3 = Led3,
        Player4 = Led4,
        Player5 = Led1 | Led2,
        Player6 = Led1 | Led3,
        Player7 = Led1 | Led4,
        Player8 = Led2 | Led3,
        Player9 = Led2 | Led4,
        Player10 = Led3 | Led4,
        Player11 = Led1 | Led2 | Led3,
        Player12 = Led1 | Led2 | Led4,
        Player13 = Led1 | Led3 | Led4,
        Player14 = Led2 | Led3 | Led4,
        Player15 = All,

        // If someone connects this many devices it's their problem that this is the same as 15 lol
        Player16 = All,
    }

    internal static class XboxGHLGuitarSetPlayerLeds
    {
        public const byte SubCommand = 0x01;

        public static unsafe XboxMessage<XboxGHLGuitarOutput> Create(XboxGHLGuitarPlayerLeds leds)
        {
            var output = new XboxGHLGuitarOutput()
            {
                SubCommand = SubCommand,
            };

            output.Data[0] = 0x08;
            output.Data[1] = (byte)leds;

            return new XboxMessage<XboxGHLGuitarOutput>()
            {
                Header = new XboxCommandHeader()
                {
                    CommandId = XboxGHLGuitarOutput.CommandId,
                    Flags = XboxCommandFlags.None,
                },
                Data = output,
            };
        }
    }

    internal class XboxGHLGuitarKeepAlive : IDisposable
    {
        public const byte SubCommand = 0x02;
        public const int SendPeriodMilliseconds = 8000;

        public static readonly XboxMessage<XboxGHLGuitarOutput> Message = CreateMessage();

        private readonly IBackendClient client;
        private readonly Timer sendTimer;

        public unsafe XboxGHLGuitarKeepAlive(IBackendClient client)
        {
            this.client = client;
            sendTimer = new Timer(SendKeepAlive, null, 0, SendPeriodMilliseconds);
        }

        ~XboxGHLGuitarKeepAlive()
        {
            Dispose(false);
        }

        private static unsafe XboxMessage<XboxGHLGuitarOutput> CreateMessage()
        {
            var output = new XboxGHLGuitarOutput()
            {
                SubCommand = SubCommand,
            };

            // Unknown magic data
            output.Data[0] = 0x08;
            output.Data[1] = 0x0A;

            return new XboxMessage<XboxGHLGuitarOutput>()
            {
                Header = new XboxCommandHeader()
                {
                    CommandId = XboxGHLGuitarOutput.CommandId,
                    Flags = XboxCommandFlags.None,
                },
                Data = output,
            };
        }

        private void SendKeepAlive(object _) => client.SendMessage(Message);

        /// <summary>
        /// Disposes the mapper and any resources it uses.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                sendTimer.Dispose();
            }
        }
    }
}