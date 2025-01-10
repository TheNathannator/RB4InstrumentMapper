using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace RB4InstrumentMapper.Parsing
{
    public static class WinUsbBackend
    {
        private static readonly DeviceNotificationListener watcher = new DeviceNotificationListener();
        private static readonly ConcurrentDictionary<string, XboxWinUsbDevice> devices = new ConcurrentDictionary<string, XboxWinUsbDevice>();

        private static bool inputsEnabled = false;

        public static int DeviceCount => devices.Count;

        public static event Action DeviceCountChanged;

        public static bool Initialized { get; private set; } = false;

        public static void Initialize()
        {
            if (Initialized)
                return;

            foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
            {
                AddDevice(deviceInfo.DevicePath);
            }

            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);

            Initialized = true;
        }

        public static void Uninitialize()
        {
            if (!Initialized)
                return;

            watcher.StopListen();
            watcher.DeviceArrived -= DeviceArrived;
            watcher.DeviceRemoved -= DeviceRemoved;

            ResetDevices();

            Initialized = false;
        }

        public static void ResetDevices()
        {
            if (!Initialized)
                return;

            foreach (var devicePath in devices.Keys)
            {
                RemoveDevice(devicePath, remove: false);
            }

            devices.Clear();
        }

        private static void DeviceArrived(DeviceEventArgs args)
        {
            AddDevice(args.SymLink);
        }

        private static void DeviceRemoved(DeviceEventArgs args)
        {
            RemoveDevice(args.SymLink);
        }

        private static void AddDevice(string devicePath)
        {
            // Paths are case-insensitive
            devicePath = devicePath.ToLowerInvariant();
            var device = XboxWinUsbDevice.TryCreate(devicePath);
            if (device == null)
                return;

            device.EnableInputs(inputsEnabled);
            device.StartReading();
            devices[devicePath] = device;

            Logging.WriteLine($"USB device {devicePath} connected");
            DeviceCountChanged?.Invoke();
        }

        private static void RemoveDevice(string devicePath, bool remove = true)
        {
            // Paths are case-insensitive
            devicePath = devicePath.ToLowerInvariant();
            if (!devices.TryGetValue(devicePath, out var device))
                return;

            device.Dispose();
            if (remove)
                devices.TryRemove(devicePath, out _);

            Logging.WriteLine($"USB device {devicePath} disconnected");
            DeviceCountChanged?.Invoke();
        }

        public static Task StartCapture()
        {
            if (!Initialized)
                return Task.CompletedTask;

            inputsEnabled = true;
            if (!devices.IsEmpty)
            {
                Logging.WriteLine("Rebooting USB devices to ensure proper startup. Hang tight...");
                Logging.WriteLine("(If this takes more than 15 seconds or so, try re-connecting your devices.)");
                return Task.Run(ResetDevices);
            }

            return Task.CompletedTask;
        }

        public static Task StopCapture()
        {
            if (!Initialized)
                return Task.CompletedTask;

            inputsEnabled = false;
            if (!devices.IsEmpty)
            {
                Logging.WriteLine("Rebooting USB devices to refresh them after mapping...");
                Logging.WriteLine("(If this takes more than 15 seconds or so, try re-connecting your devices.)");
                return Task.Run(ResetDevices);
            }

            return Task.CompletedTask;
        }

        // WinUSB devices are exclusive-access, so we need a helper method to get already-initialized devices
        public static USBDevice GetUsbDevice(string devicePath)
        {
            if (devices.TryGetValue(devicePath, out var device))
                return device.UsbDevice;
            return USBDevice.GetSingleDeviceByPath(devicePath);
        }

        public static bool SwitchDeviceToWinUSB(string instanceId)
        {
            try
            {
                var device = PnPDevice.GetDeviceByInstanceId(instanceId).ToUsbPnPDevice();
                return SwitchDeviceToWinUSB(device);
            }
            catch (Exception ex)
            {
                // Verbose since this will be attempted twice, and the first attempt will always fail if we're not elevated
                Logging.WriteExceptionVerbose($"Failed to switch device {instanceId} to WinUSB!", ex);
                return false;
            }
        }

        public static bool SwitchDeviceToWinUSB(UsbPnPDevice device)
        {
            try
            {
                if (!XboxWinUsbDevice.IsXGIPDevice(device))
                {
                    Debug.Fail($"Device instance {device.InstanceId} is not an XGIP device!");
                    return false;
                }

                device.InstallNullDriver(out bool reboot);
                if (reboot)
                    device.CyclePort();

                device.InstallCustomDriver("winusb.inf", out reboot);
                if (reboot)
                    device.CyclePort();

                return true;
            }
            catch (Exception ex)
            {
                // Verbose since this will be attempted twice, and the first attempt will always fail if we're not elevated
                Logging.WriteExceptionVerbose($"Failed to switch device {device.InstanceId} to WinUSB!", ex);
                return false;
            }
        }

        public static bool RevertDevice(string instanceId)
        {
            try
            {
                var device = PnPDevice.GetDeviceByInstanceId(instanceId).ToUsbPnPDevice();
                return RevertDevice(device);
            }
            catch (Exception ex)
            {
                // Verbose since this will be attempted twice, and the first attempt will always fail if we're not elevated
                Logging.WriteExceptionVerbose($"Failed to revert device {instanceId} to its original driver!", ex);
                return false;
            }
        }

        public static bool RevertDevice(UsbPnPDevice device)
        {
            try
            {
                device.InstallNullDriver(out bool reboot);
                if (reboot)
                    device.CyclePort();

                device.Uninstall(out reboot);
                if (reboot)
                    device.CyclePort();

                return Devcon.Refresh();
            }
            catch (Exception ex)
            {
                // Verbose since this will be attempted twice: once in-process, and once in a separate elevated process
                Logging.WriteExceptionVerbose($"Failed to revert device {device.InstanceId} to its original driver!", ex);
                return false;
            }
        }
    }
}