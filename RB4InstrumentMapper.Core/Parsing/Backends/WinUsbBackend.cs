using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace RB4InstrumentMapper.Core.Parsing
{
    public static class WinUsbBackend
    {
        private static readonly Guid WinUsbClassGuid = Guid.Parse("88BAE032-5A81-49F0-BC3D-A4FF138216D6");
        private const string XGIP_COMPATIBLE_ID = @"USB\MS_COMP_XGIP10";

        private static readonly DeviceNotificationListener watcher = new DeviceNotificationListener();
        private static readonly ConcurrentDictionary<string, XboxWinUsbDevice> devices = new ConcurrentDictionary<string, XboxWinUsbDevice>();

        private static bool inputsEnabled = false;

        public static int DeviceCount => devices.Count;

        public static event Action DeviceCountChanged;

        public static bool Initialized { get; private set; } = false;

        public static bool Initialize()
        {
            if (Initialized)
                return true;

            try
            {
                foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
                {
                    AddDevice(deviceInfo.DevicePath);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException("Failed to initialize WinUSB backend!", ex);
                ClearDevices();
                return false;
            }

            DeviceCountChanged?.Invoke();

            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);

            Initialized = true;
            return true;
        }

        public static void Uninitialize()
        {
            if (!Initialized)
                return;

            watcher.StopListen();
            watcher.DeviceArrived -= DeviceArrived;
            watcher.DeviceRemoved -= DeviceRemoved;

            ClearDevices();

            Initialized = false;
        }

        private static void ClearDevices()
        {
            if (!Initialized)
                return;

            foreach (var devicePath in devices.Keys)
            {
                RemoveDevice(devicePath, remove: false);
            }

            devices.Clear();

            DeviceCountChanged?.Invoke();
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
            if (!IsCompatibleDevice(devicePath))
                return;

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

        public static void StartCapture()
        {
            if (!Initialized)
                return;

            inputsEnabled = true;
            foreach (var device in devices.Values)
            {
                device.EnableInputs(inputsEnabled);
            }
        }

        public static void StopCapture()
        {
            if (!Initialized)
                return;

            inputsEnabled = false;
            foreach (var device in devices.Values)
            {
                device.EnableInputs(inputsEnabled);
            }
        }

        public static bool IsCompatibleDevice(string devicePath)
        {
            try
            {
                var device = PnPDevice.GetDeviceByInterfaceId(devicePath);
                return IsCompatibleDevice(device);
            }
            catch (Exception ex)
            {
                Logging.WriteException("Failed to determine device compatibility!", ex);
                return false;
            }
        }

        public static bool IsCompatibleDevice(PnPDevice device)
        {
            try
            {
                // Only accept WinUSB devices, at least for now
                var classGuid = device.GetProperty<Guid>(DevicePropertyKey.Device_ClassGuid);
                if (classGuid != WinUsbClassGuid)
                    return false;

                // Check for the Xbox One compatible ID
                if (!IsXGIPDevice(device))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Logging.WriteException("Failed to determine device compatibility!", ex);
                return false;
            }
        }

        public static bool IsXGIPDevice(PnPDevice device)
        {
            // Check for the Xbox One compatible ID
            var compatibleIds = device.GetProperty<string[]>(DevicePropertyKey.Device_CompatibleIds);
            foreach (string id in compatibleIds)
            {
                if (id == XGIP_COMPATIBLE_ID)
                    return true;
            }

            return false;
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
                if (!IsXGIPDevice(device))
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