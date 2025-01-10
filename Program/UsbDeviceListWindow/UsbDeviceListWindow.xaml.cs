using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;
using RB4InstrumentMapper.Parsing;
using RB4InstrumentMapper.Properties;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Interaction logic for UsbDeviceListWindow.xaml
    /// </summary>
    public partial class UsbDeviceListWindow : Window
    {
        private readonly DeviceNotificationListener watcher = new DeviceNotificationListener();

        private bool switchingDriver = false;

        public UsbDeviceListWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            watcher.DeviceArrived += DeviceArrived;
            watcher.DeviceRemoved += DeviceRemoved;
            watcher.StartListen(DeviceInterfaceIds.UsbDevice);

            Refresh();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = switchingDriver;
            base.OnClosing(e);
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            watcher.StopListen();
            watcher.Dispose();
        }

        private void DriverSwitchStart(XboxUsbDeviceControl sender)
        {
            switchingDriver = true;
            // This hides the entire title bar. Not the prettiest, but properly disabling
            // the close button requires calling into native, and I don't wanna do that just for this window lol
            WindowStyle = WindowStyle.None;

            DisableAllEntries(winUsbDeviceList);
            DisableAllEntries(xgipDeviceList);
        }

        private void DriverSwitchEnd(XboxUsbDeviceControl sender)
        {
            switchingDriver = false;
            WindowStyle = WindowStyle.SingleBorderWindow;

            // Bit redundant, but w/e lol
            EnableAllEntries(winUsbDeviceList);
            EnableAllEntries(xgipDeviceList);

            Refresh();
        }

        private void EnableAllEntries(StackPanel devices)
        {
            foreach (XboxUsbDeviceControl entry in devices.Children)
            {
                entry.Enable();
            }

            devices.UpdateLayout();
        }

        private void DisableAllEntries(StackPanel devices)
        {
            foreach (XboxUsbDeviceControl entry in devices.Children)
            {
                entry.Disable();
            }

            devices.UpdateLayout();
        }

        private void Refresh()
        {
            RemoveAll(winUsbDeviceList);
            RemoveAll(xgipDeviceList);

            foreach (var deviceInfo in USBDevice.GetDevices(DeviceInterfaceIds.UsbDevice))
            {
                AddDevice(deviceInfo.DevicePath);
            }
        }

        private void DeviceArrived(DeviceEventArgs args)
        {
            if (switchingDriver)
                return;

            Dispatcher.BeginInvoke(new Action(() => AddDevice(args.SymLink)));
        }

        private void DeviceRemoved(DeviceEventArgs args)
        {
            if (switchingDriver)
                return;

            Dispatcher.BeginInvoke(new Action(() => RemoveDevice(args.SymLink)));
        }

        private void AddDevice(string devicePath)
        {
            try
            {
                devicePath = devicePath.ToLower();
                var pnpDevice = PnPDevice.GetDeviceByInterfaceId(devicePath).ToUsbPnPDevice();

                if (XboxWinUsbDevice.IsCompatibleDevice(pnpDevice))
                {
                    var deviceControl = new XboxUsbDeviceControl(devicePath, pnpDevice, winusb: true);
                    deviceControl.DriverSwitchStart += DriverSwitchStart;
                    deviceControl.DriverSwitchEnd += DriverSwitchEnd;
                    winUsbDeviceList.Children.Add(deviceControl);
                }
                else if (XboxWinUsbDevice.IsXGIPDevice(pnpDevice))
                {
                    var deviceControl = new XboxUsbDeviceControl(devicePath, pnpDevice, winusb: false);
                    deviceControl.DriverSwitchStart += DriverSwitchStart;
                    deviceControl.DriverSwitchEnd += DriverSwitchEnd;
                    xgipDeviceList.Children.Add(deviceControl);
                }
            }
            catch (Exception ex)
            {
                Logging.Main_WriteException(ex, $"Failed to add device {devicePath} to USB device window!");
                return;
            }
        }

        private void RemoveDevice(string devicePath)
        {
            devicePath = devicePath.ToLower();

            RemoveDevice(devicePath, winUsbDeviceList);
            RemoveDevice(devicePath, xgipDeviceList);
        }

        private void RemoveDevice(string devicePath, StackPanel devices)
        {
            var children = devices.Children;
            // By index, since we need to modify the list
            for (int i = 0; i < children.Count; i++)
            {
                var entry = (XboxUsbDeviceControl)children[i];
                if (entry.DevicePath == devicePath)
                {
                    entry.DriverSwitchStart -= DriverSwitchStart;
                    entry.DriverSwitchEnd -= DriverSwitchEnd;

                    children.RemoveAt(i);
                    // Continue through everything to ensure removal
                    // For some reason, things don't get removed correctly otherwise
                    i--;
                }
            }

            devices.UpdateLayout();
        }

        private void RemoveAll(StackPanel devices)
        {
            foreach (XboxUsbDeviceControl entry in devices.Children)
            {
                entry.DriverSwitchStart -= DriverSwitchStart;
                entry.DriverSwitchEnd -= DriverSwitchEnd;
            }

            devices.Children.Clear();
            devices.UpdateLayout();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            MessageBox.Show(
                "Manually switching device drivers is for advanced users, and must be performed carefully. " +
                "You are responsible for any problems that may arise from this process, and you are largely on your own if something bugs out.\n\n" +
                "You have been warned!",
                "DISCLAIMER",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
