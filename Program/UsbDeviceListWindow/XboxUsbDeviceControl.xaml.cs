using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Nefarius.Utilities.DeviceManagement.PnP;
using RB4InstrumentMapper.Parsing;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Interaction logic for XboxUsbDeviceControl.xaml
    /// </summary>
    public partial class XboxUsbDeviceControl : UserControl
    {
        public event Action<XboxUsbDeviceControl> DriverSwitchStart;
        public event Action<XboxUsbDeviceControl> DriverSwitchEnd;

        public string DevicePath { get; }
        public UsbPnPDevice PnpDevice { get; }
        public bool IsWinUsb { get; }

        public XboxUsbDeviceControl(string devicePath, UsbPnPDevice device, bool winusb)
        {
            DevicePath = devicePath;
            PnpDevice = device;
            IsWinUsb = winusb;

            InitializeComponent();

            if (IsWinUsb)
            {
                try
                {
                    var usbDevice = WinUsbBackend.GetUsbDevice(devicePath);
                    manufacturerLabel.Content = usbDevice.Descriptor.Manufacturer;
                    nameLabel.Content = usbDevice.Descriptor.Product;
                }
                catch (Exception ex)
                {
                    Logging.Main_WriteException(ex, $"Failed to get USB name/manufacturer for device '{devicePath}'!");
                    manufacturerLabel.Content = "(Failed to get manufacturer)";
                    nameLabel.Content = "(Failed to get name)";
                }

                switchDriverButton.Content = "Revert Driver";
                xboxIconImage.Visibility = Visibility.Hidden;
                usbIconImage.Visibility = Visibility.Visible;
            }
            else
            {
                try
                {
                    manufacturerLabel.Content = PnpDevice.GetProperty<string>(DevicePropertyKey.Device_Manufacturer);
                    nameLabel.Content = PnpDevice.GetProperty<string>(DevicePropertyKey.NAME);
                }
                catch (Exception ex)
                {
                    Logging.Main_WriteException(ex, $"Failed to get name/manufacturer for device '{devicePath}'!");
                    manufacturerLabel.Content = "(Failed to get manufacturer)";
                    nameLabel.Content = "(Failed to get name)";
                }

                switchDriverButton.Content = "Switch Driver";
                xboxIconImage.Visibility = Visibility.Visible;
                usbIconImage.Visibility = Visibility.Hidden;
            }
        }

        public void Enable()
        {
            switchDriverButton.IsEnabled = true;
        }

        public void Disable()
        {
            switchDriverButton.IsEnabled = false;
        }

        private async void switchDriverButton_Clicked(object sender, RoutedEventArgs args)
        {
            DriverSwitchStart?.Invoke(this);
            switchDriverProgress.Visibility = Visibility.Visible;

            if (IsWinUsb)
                await RevertDriver();
            else
                await SwitchToWinUSB();

            switchDriverProgress.Visibility = Visibility.Hidden;
            DriverSwitchEnd?.Invoke(this);
        }

        private async Task SwitchToWinUSB()
        {
            // Attempt normally in case we already have admin permissions
            if (await Task.Run(() => WinUsbBackend.SwitchDeviceToWinUSB(PnpDevice)))
                return;

            // Otherwise, do it in a separate admin process
            if (!await Program.StartWinUsbProcess(PnpDevice.InstanceId))
                MessageBox.Show("Failed to switch device to WinUSB!", "Failed To Switch Driver", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private async Task RevertDriver()
        {
            // Attempt normally in case we already have admin permissions
            if (await Task.Run(() => WinUsbBackend.RevertDevice(PnpDevice)))
                return;

            // Otherwise, do it in a separate admin process
            if (!await Program.StartRevertProcess(PnpDevice.InstanceId))
            {
                var response = MessageBox.Show(
                    "Failed to revert device to its original driver!\n\nDo you want to open the manual uninstallation guide?",
                    "Failed To Switch Driver",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error
                );

                if (response == MessageBoxResult.Yes)
                {
                    Process.Start("https://github.com/TheNathannator/RB4InstrumentMapper/blob/main/Docs/WinUSB/manual-winusb-install.md#remove-winusb");
                }
            }
        }
    }
}
