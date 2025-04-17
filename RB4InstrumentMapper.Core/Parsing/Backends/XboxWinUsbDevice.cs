using System;
using System.Threading;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace RB4InstrumentMapper.Core.Parsing
{
    internal class XboxWinUsbDevice : XboxDevice
    {
        private const byte XBOX_INTERFACE_CLASS = 0xFF; // Vendor-specific
        private const byte XBOX_INTERFACE_SUB_CLASS = 0x47;
        private const byte XBOX_INTERFACE_PROTOCOL = 0xD0;

        public USBDevice UsbDevice { get; private set; }

        private USBInterface mainInterface;

        private Thread readThread;
        private volatile bool readPackets = false;
        private volatile bool ioError = false;

        private volatile bool inputsEnabled = false;
        private volatile bool previousInputsEnabled = false;

        private XboxWinUsbDevice(USBDevice usb, USBInterface @interface)
            : base(BackendType.Usb, mapGuide: true, @interface.OutPipe.MaximumPacketSize)
        {
            UsbDevice = usb;
            mainInterface = @interface;
        }

        public static XboxWinUsbDevice TryCreate(string devicePath)
        {
            try
            {
                var usbDevice = USBDevice.GetSingleDeviceByPath(devicePath);
                var mainInterface = FindMainInterface(usbDevice);
                if (mainInterface == null)
                {
                    usbDevice.Dispose();
                    return null;
                }

                return new XboxWinUsbDevice(usbDevice, mainInterface);
            }
            catch (Exception ex)
            {
                Logging.WriteException("Failed to create WinUSB device!", ex);
                return null;
            }
        }

        public void StartReading()
        {
            if (readPackets)
                return;

            ioError = false;
            readPackets = true;
            readThread = new Thread(ReadThread) { IsBackground = true };
            readThread.Start();
        }

        public void StopReading()
        {
            // Abort in pipe
            if (!ioError)
            {
                try
                {
                    mainInterface.InPipe.Abort();
                }
                catch (Exception ex)
                {
                    Logging.WriteExceptionVerbose($"Failed to abort read pipe!", ex);
                }
            }

            // Reset device
            SendMessage(XboxConfiguration.ResetDevice);

            if (!readPackets)
                return;

            readPackets = false;
            readThread.Join();
            readThread = null;
        }

        public override void EnableInputs(bool enabled)
        {
            // Defer to read thread
            inputsEnabled = enabled;
        }

        private void ReadThread()
        {
            Span<byte> readBuffer = stackalloc byte[mainInterface.InPipe.MaximumPacketSize];

            while (readPackets)
            {
                // Read packet data
                int bytesRead = ReadPacket(readBuffer);
                if (bytesRead < 0)
                    break;

                bool enabled = inputsEnabled;
                if (enabled != previousInputsEnabled)
                {
                    previousInputsEnabled = enabled;
                    base.EnableInputs(enabled);
                }

                // Process packet data
                var packetData = readBuffer.Slice(0, bytesRead);
                var result = HandleRawPacket(packetData);
                switch (result)
                {
                    case XboxResult.Success:
                        break;

                    case XboxResult.UnsupportedDevice:
                        SendMessage(XboxConfiguration.PowerOffDevice);
                        readPackets = false;
                        break;
                }
            }

            readPackets = false;
        }

        private int ReadPacket(Span<byte> readBuffer)
        {
            if (ioError)
                return -1;

            const int retryThreshold = 3;
            int retryCount = 0;

            do
            {
                try
                {
                    return mainInterface.InPipe.Read(readBuffer);
                }
                catch (Exception ex)
                {
                    Logging.WriteExceptionVerbose($"Error while reading packet! (Attempt {retryCount + 1})", ex);
                }
            }
            while (++retryCount < retryThreshold);

            ioError = true;
            return -1;
        }

        protected override XboxResult SendPacket(Span<byte> data)
        {
            if (ioError)
                return XboxResult.Disconnected;

            const int retryThreshold = 3;
            int retryCount = 0;

            do
            {
                try
                {
                    mainInterface.OutPipe.Write(data);
                    return XboxResult.Success;
                }
                catch (Exception ex)
                {
                    Logging.WriteExceptionVerbose($"Error while sending packet! (Attempt {retryCount + 1})", ex);
                }
            }
            while (++retryCount < retryThreshold);

            ioError = true;
            return XboxResult.Disconnected;
        }

        private static USBInterface FindMainInterface(USBDevice device)
        {
            foreach (var iface in device.Interfaces)
            {
                // Ignore non-XGIP interfaces
                if (iface.ClassValue != XBOX_INTERFACE_CLASS ||
                    iface.SubClass != XBOX_INTERFACE_SUB_CLASS ||
                    iface.Protocol != XBOX_INTERFACE_PROTOCOL)
                    continue;

                // The main interface uses interrupt transfers
                if (iface.InPipe?.TransferType != USBTransferType.Interrupt ||
                    iface.OutPipe?.TransferType != USBTransferType.Interrupt)
                    continue;

                return iface;
            }

            return null;
        }

        protected override void ReleaseManagedResources()
        {
            base.ReleaseManagedResources();

            if (readThread != null)
                StopReading();

            UsbDevice?.Dispose();
            UsbDevice = null;
            mainInterface = null;
        }
    }
}