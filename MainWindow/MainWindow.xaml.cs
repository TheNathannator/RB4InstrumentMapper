using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using vJoyInterfaceWrap;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using SharpPcap;
using SharpPcap.LibPcap;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Dispatcher to send changes to UI.
        /// </summary>
        private static Dispatcher uiDispatcher = null;

        /// <summary>
        /// List of available Pcap devices.
        /// </summary>
        private CaptureDeviceList pcapDeviceList = null;

        /// <summary>
        /// The selected Pcap device.
        /// </summary>
        private ILiveDevice pcapSelectedDevice = null;

        /// <summary>
        /// Flag indicating that packet capture is active.
        /// </summary>
        private bool packetCaptureActive = false;

        /// <summary>
        /// Flag indicating if packets should be shown.
        /// </summary>
        private bool packetDebug = false;

        /// <summary>
        /// Flag indicating if packets should be logged to a file.
        /// </summary>
        private bool packetDebugLog = false;

        /// <summary>
        /// Common name for Pcap combo box items.
        /// </summary>
        private const string pcapComboBoxItemName = "pcapDeviceComboBoxItem";

        /// <summary>
        /// vJoy client.
        /// </summary>
        private static readonly vJoy vjoy = new vJoy();

        /// <summary>
        /// Counter for processed packets.
        /// </summary>
        private ulong processedPacketCount = 0;

        private enum ControllerType
        {
            None = -1,
            vJoy = 0,
            VigemBus = 1
        }

        /// <summary>
        /// Initializes a new MainWindow.
        /// </summary>
        public MainWindow()
        {
            // Assign event handler for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            InitializeComponent();

            Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            versionLabel.Content = $"v{version.ToString()}";
#if DEBUG
            versionLabel.Content += " Debug";
#endif

            // Capture Dispatcher object for use in callback
            uiDispatcher = this.Dispatcher;
        }

        /// <summary>
        /// Called when the window loads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to console
            TextBoxConsole.RedirectConsoleToTextBox(messageConsole, displayLinesWithTimestamp: false);

            // Load saved settings
            packetDebugCheckBox.IsChecked = Properties.Settings.Default.packetDebug;
            packetLogCheckBox.IsChecked = Properties.Settings.Default.packetDebugLog;
            int deviceType = controllerDeviceTypeCombo.SelectedIndex = Properties.Settings.Default.controllerDeviceType;

            // Check for vJoy
            bool vjoyFound = vjoy.vJoyEnabled();
            if (vjoyFound)
            {
                // Log vJoy driver attributes (Vendor Name, Product Name, Version Number)
                Console.WriteLine("vJoy found! - Vendor: " + vjoy.GetvJoyManufacturerString() + ", Product: " + vjoy.GetvJoyProductString() + ", Version Number: " + vjoy.GetvJoySerialNumberString());
                if (CountAvailableVjoyDevices() > 0)
                {
                    (controllerDeviceTypeCombo.Items[0] as ComboBoxItem).IsEnabled = true;
                }
                else
                {
                    // Reset device type selection if it was set to vJoy
                    if (deviceType == (int)ControllerType.vJoy)
                    {
                        controllerDeviceTypeCombo.SelectedIndex = (int)ControllerType.None;
                    }
                }
            }
            else
            {
                Console.WriteLine("No vJoy driver found, or vJoy is disabled. vJoy selection will be unavailable.");

                // Reset device type selection if it was set to vJoy
                if (deviceType == (int)ControllerType.vJoy)
                {
                    controllerDeviceTypeCombo.SelectedIndex = (int)ControllerType.None;
                }
            }

            // Check for ViGEmBus
            bool vigemFound;
            try
            {
                var vigem = new ViGEmClient();
                vigemFound = true;
                Console.WriteLine("ViGEmBus found!");
                vigem.Dispose();
                (controllerDeviceTypeCombo.Items[1] as ComboBoxItem).IsEnabled = true;
            }
            catch (VigemBusNotFoundException)
            {
                vigemFound = false;
                Console.WriteLine("ViGEmBus not found. ViGEmBus selection will be unavailable.");

                // Reset device type selection if it was set to ViGEmBus
                if (deviceType == (int)ControllerType.vJoy)
                {
                    controllerDeviceTypeCombo.SelectedIndex = (int)ControllerType.None;
                }
            }

            // Exit if neither ViGEmBus nor vJoy are installed
            if (!(vjoyFound || vigemFound))
            {
                MessageBox.Show("No controller emulators found! Please install vJoy or ViGEmBus.\nThe program will now shut down.", "No Controller Emulators Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // Initialize Pcap dropdown
            PopulatePcapDropdown();
        }

        /// <summary>
        /// Called when the window has closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Shutdown
            if (packetCaptureActive)
            {
                StopCapture();
            }

            // Close the log files
            Logging.CloseAll();
        }

        /// <summary>
        /// Populates the Pcap device combo.
        /// </summary>
        /// <remarks>
        /// Used both when initializing, and when refreshing.
        /// </remarks>
        private void PopulatePcapDropdown()
        {
            // Clear combo list
            pcapDeviceCombo.Items.Clear();

            // Retrieve the device list from the local machine
            pcapDeviceList = CaptureDeviceList.Instance;

            if (pcapDeviceList.Count == 0)
            {
                Console.WriteLine("No Pcap devices found!");
                return;
            }

            // Load saved device name
            string currentPcapSelection = Properties.Settings.Default.pcapDevice;

            // Populate combo and print the list
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pcapDeviceList.Count; i++)
            {
                ILiveDevice device = pcapDeviceList[i];
                string itemNumber = $"{i + 1}";

                sb.Clear();
                sb.Append($"{itemNumber}. ");
                if (device.Description != null)
                {
                    sb.Append(device.Description);
                    sb.Append($" ({device.Name})");
                }
                else
                {
                    sb.Append(device.Name);
                }

                string deviceName = sb.ToString();
                string itemName = pcapComboBoxItemName + itemNumber;
                bool isSelected = device.Name.Equals(currentPcapSelection) || device.Name.Equals(pcapSelectedDevice?.Name);

                if (isSelected)
                {
                    pcapSelectedDevice = device;
                }

                pcapDeviceCombo.Items.Add(new ComboBoxItem()
                {
                    Name = itemName,
                    Content = deviceName,
                    IsEnabled = true,
                    IsSelected = isSelected
                });
            }

            // Set selection to nothing if saved device not detected
            if (pcapSelectedDevice == null)
            {
                pcapDeviceCombo.SelectedIndex = -1;
            }

            Console.WriteLine($"Discovered {pcapDeviceList.Count} Pcap devices.");
        }

        /// <summary>
        /// Populates the controller device list as vJoy.
        /// </summary>
        private int CountAvailableVjoyDevices()
        {
            if (!vjoy.vJoyEnabled())
            {
                return 0;
            }

            // Loop through vJoy IDs and populate list
            int freeDeviceCount = 0;
            for (uint id = 1; id <= 16; id++)
            {
                if (vjoy.GetVJDStatus(id) == VjdStat.VJD_STAT_FREE)
                {
                    // Check that the vJoy device is configured correctly
                    int numButtons = vjoy.GetVJDButtonNumber(id);
                    int numContPov = vjoy.GetVJDContPovNumber(id);
                    bool xExists = vjoy.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X); // X axis
                    bool yExists = vjoy.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y); // Y axis
                    bool zExists = vjoy.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z); // Z axis

                    if (numButtons >= 16 &&
                        numContPov >= 1 &&
                        xExists &&
                        yExists &&
                        zExists
                    )
                    {
                        freeDeviceCount++;
                    }
                }
            }

            switch (freeDeviceCount)
            {
                case 0:
                    Console.WriteLine($"No vJoy devices available! Please configure some in the Configure vJoy application.");
                    Console.WriteLine($"Devices must be configured with 16 or more buttons, 1 or more continuous POV hats, and have the X, Y, and Z axes.");
                    break;

                case 1:
                    Console.WriteLine($"{freeDeviceCount} vJoy device available.");
                    break;

                default:
                    Console.WriteLine($"{freeDeviceCount} vJoy devices available.");
                    break;
            }

            return freeDeviceCount;
        }

        private void SetStartButtonEnabled()
        {
            startButton.IsEnabled = (
                controllerDeviceTypeCombo.SelectedIndex != (int)ControllerType.None &&
                pcapDeviceCombo.SelectedIndex != -1
            );
        }

        /// <summary>
        /// Configures the Pcap device and controller devices, and starts packet capture.
        /// </summary>
        private void StartCapture()
        {
            // Check if a device has been selected
            if (pcapSelectedDevice == null)
            {
                Console.WriteLine("Please select a Pcap device from the Pcap dropdown.");
                return;
            }

            // Check if the device is still present
            bool deviceStillPresent = false;
            foreach (var device in CaptureDeviceList.Instance)
            {
                if (device.Name == pcapSelectedDevice.Name)
                {
                    deviceStillPresent = true;
                    break;
                }
            }

            if (!deviceStillPresent)
            {
                // Invalidate selected device (but not the saved preference)
                pcapSelectedDevice = null;

                // Notify user
                MessageBox.Show(
                    "Pcap device list has changed and the selected device is no longer present.\nPlease re-select your device from the list and try again.",
                    "Pcap Device Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation
                );

                // Force a refresh
                PopulatePcapDropdown();
                return;
            }

            // Enable packet capture active flag
            packetCaptureActive = true;

            // Set window controls
            pcapDeviceCombo.IsEnabled = false;
            pcapAutoDetectButton.IsEnabled = false;
            pcapRefreshButton.IsEnabled = false;
            packetDebugCheckBox.IsEnabled = false;
            packetLogCheckBox.IsEnabled = false;

            controllerDeviceTypeCombo.IsEnabled = false;

            startButton.Content = "Stop";

            // Enable packet count display
            packetsProcessedLabel.Visibility = Visibility.Visible;
            packetsProcessedCountLabel.Visibility = Visibility.Visible;
            packetsProcessedCountLabel.Content = "0";
            processedPacketCount = 0;

            // Initialize packet log
            if (packetDebugLog)
            {
                if (!Logging.CreatePacketLog())
                {
                    packetDebugLog = false;
                    Console.WriteLine("Disabled packet logging for this capture session.");
                }
            }

            // Register handler for packet debugging/logging and status reporting
            pcapSelectedDevice.OnPacketArrival += OnPacketArrival;

            // Open the device
            pcapSelectedDevice.Open(new DeviceConfiguration()
            {
                Snaplen = 45, // Capture small packets
                Mode = DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, // Promiscuous mode with maximum speed
                ReadTimeout = 50 // Read timeout
            });

            // Configure packet receive event handler
            pcapSelectedDevice.OnPacketArrival += OnPacketArrival;
            
            // Start capture
            pcapSelectedDevice.StartCapture();
            Console.WriteLine($"Listening on {pcapSelectedDevice.Description}...");
        }

        /// <summary>
        /// Stops packet capture/mapping and resets Pcap/controller objects.
        /// </summary>
        private void StopCapture()
        {
            // Stop packet capture
            if (pcapSelectedDevice != null)
            {
                // Close will automatically remove assigned event handlers and call StopCapture(), 
                // so we don't need to do those ourselves
                pcapSelectedDevice.Close();
            }

            PacketParser.Close();

            // Store whether or not the packet log was created
            bool doPacketLogMessage = Logging.PacketLogExists;
            // Close packet log file
            Logging.ClosePacketLog();
            
            // Disable packet capture active flag
            packetCaptureActive = false;

            // Set window controls
            pcapDeviceCombo.IsEnabled = true;
            pcapAutoDetectButton.IsEnabled = true;
            pcapRefreshButton.IsEnabled = true;
            packetDebugCheckBox.IsEnabled = true;
            packetLogCheckBox.IsEnabled = true;

            controllerDeviceTypeCombo.IsEnabled = true;

            startButton.Content = "Start";

            // Disable packet count display
            packetsProcessedLabel.Visibility = Visibility.Hidden;
            packetsProcessedCountLabel.Visibility = Visibility.Hidden;
            packetsProcessedCountLabel.Content = string.Empty;
            processedPacketCount = 0;

            // Force a refresh of the controller textbox
            controllerDeviceTypeCombo_SelectionChanged(null, null);

            Console.WriteLine("Stopped capture.");
            if (doPacketLogMessage)
            {
                Console.WriteLine($"Packet logs may be found in {Logging.PacketLogFolderPath}");
            }
        }

        /// <summary>
        /// Handles captured packets.
        /// </summary>
        /// <param name="packet">The received packet</param>
        private void OnPacketArrival(object sender, PacketCapture packet)
        {
            // Debugging (if enabled)
            if (packetDebug)
            {
                RawCapture raw = packet.GetPacket();
                string packetLogString = raw.Timeval.Date.ToString("yyyy-MM-dd hh:mm:ss.fff") + $" [{raw.PacketLength}] " + ParsingHelpers.ByteArrayToHexString(raw.Data);;
                Console.WriteLine(packetLogString);
                Logging.Packet_WriteLine(packetLogString);
            }

            try
            {
                PacketParser.HandlePcapPacket(packet.Data, ref processedPacketCount);
            }
            catch (ThreadAbortException)
            {
                // Don't log ThreadAbortExceptions, just return
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while handling packet: {ex.GetFirstLine()}");
                Logging.Main_WriteException(ex, "Context: Unhandled error during packet handling");

                // Stop capture
                uiDispatcher.Invoke(StopCapture);
                return;
            }

            // Status reporting (slow)
            if ((processedPacketCount < 10) ||
               ((processedPacketCount < 100) && (processedPacketCount % 10 == 0)) ||
                (processedPacketCount % 100 == 0))
            {
                // Update UI
                uiDispatcher.Invoke(() =>
                {
                    string ulongString = processedPacketCount.ToString("N0");
                    packetsProcessedCountLabel.Content = ulongString;
                });
            }
        }

        /// <summary>
        /// Handles Pcap device selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected combo box item
            ComboBoxItem selection = pcapDeviceCombo.SelectedItem as ComboBoxItem;
            if (selection == null)
            {
                // Disable start button
                startButton.IsEnabled = false;

                // Clear saved device
                Properties.Settings.Default.pcapDevice = String.Empty;
                Properties.Settings.Default.Save();
                return;
            }
            string itemName = selection.Name;

            // Get index of selected Pcap device
            int pcapDeviceIndex = -1;
            if (int.TryParse(itemName.Substring(pcapComboBoxItemName.Length), out pcapDeviceIndex))
            {
                // Adjust index count (UI->Logical)
                pcapDeviceIndex -= 1;

                // Assign device
                pcapSelectedDevice = pcapDeviceList[pcapDeviceIndex];
                Console.WriteLine($"Selected Pcap device {pcapSelectedDevice.Description}");

                // Enable start button
                SetStartButtonEnabled();

                // Remember selected Pcap device's name
                Properties.Settings.Default.pcapDevice = pcapSelectedDevice.Name;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handles the click of the Start button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (!packetCaptureActive)
            {
                StartCapture();
            }
            else
            {
                StopCapture();
            }
        }

        /// <summary>
        /// Handles the packet debug checkbox being checked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetDebugCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            packetDebug = true;
            PacketParser.PacketDebug = true;
            packetLogCheckBox.IsEnabled = true;
            packetDebugLog = packetLogCheckBox.IsChecked.GetValueOrDefault();

            // Remember selected packet debug state
            Properties.Settings.Default.packetDebug = true;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Handles the packet debug checkbox being unchecked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetDebugCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            packetDebug = false;
            PacketParser.PacketDebug = false;
            packetLogCheckBox.IsEnabled = false;
            packetDebugLog = false;

            // Remember selected packet debug state
            Properties.Settings.Default.packetDebug = false;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Handles the packet debug checkbox being checked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetLogCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            packetDebugLog = true;

            // Remember selected packet debug state
            Properties.Settings.Default.packetDebugLog = true;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Handles the packet debug checkbox being unchecked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetLogCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            packetDebugLog = false;

            // Remember selected packet debug state
            Properties.Settings.Default.packetDebugLog = false;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Handles the click of the Pcap Refresh button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Re-populate dropdown
            PopulatePcapDropdown();
        }

        private void controllerDeviceTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Set parsing mode
            switch (controllerDeviceTypeCombo.SelectedIndex)
            {
                // vJoy
                case 0:
                    if (CountAvailableVjoyDevices() > 0)
                    {
                        PacketParser.ParseMode = ParsingMode.vJoy;
                        Properties.Settings.Default.controllerDeviceType = (int)ControllerType.vJoy;
                    }
                    else
                    {
                        // Reset device type selection
                        // The parse mode and saved setting will get set automatically since setting this fires off this handler again
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                // ViGEmBus
                case 1:
                    PacketParser.ParseMode = ParsingMode.ViGEmBus;
                    Properties.Settings.Default.controllerDeviceType = (int)ControllerType.VigemBus;
                    break;

                default:
                    PacketParser.ParseMode = (ParsingMode)0;
                    Properties.Settings.Default.controllerDeviceType = (int)ControllerType.None;
                    break;
            }

            // Save setting
            Properties.Settings.Default.controllerDeviceType = controllerDeviceTypeCombo.SelectedIndex;
            Properties.Settings.Default.Save();

            // Enable start button
            SetStartButtonEnabled();
        }

        /// <summary>
        /// Handles the Pcap auto-detect button being clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapAutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result;

            // Prompt user to unplug their receiver
            result = MessageBox.Show(
                "Unplug your receiver, then click OK.\n(A 1-second delay will be taken to ensure that it registers as disconnected.)",
                "Auto-Detect Receiver",
                MessageBoxButton.OKCancel
            );
            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            // Wait for a moment before getting the new list to ensure the device is registered
            Thread.Sleep(1000);

            // Get the list of devices for when receiver is unplugged
            CaptureDeviceList notPlugged = CaptureDeviceList.Instance;

            // Prompt user to plug in their receiver
            result = MessageBox.Show(
                "Now plug in your receiver, wait a bit for it to register, then click OK.\n(A 1-second delay will be taken to ensure that it registers as connected.)",
                "Auto-Detect Receiver",
                MessageBoxButton.OKCancel
            );
            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            // Wait for a moment before getting the new list to ensure the device is registered
            Thread.Sleep(1000);

            // Get the list of devices for when receiver is plugged in
            CaptureDeviceList plugged = CaptureDeviceList.Instance;

            // Get device names for both not plugged and plugged lists
            List<string> notPluggedNames = new List<string>();
            List<string> pluggedNames = new List<string>();
            foreach (ILiveDevice oldDevice in notPlugged)
            {
                notPluggedNames.Add(oldDevice.Name);
            }
            foreach (ILiveDevice newDevice in plugged)
            {
                pluggedNames.Add(newDevice.Name);
            }

            // Compare the lists and find what notPlugged doesn't contain
            List<string> newNames = new List<string>();
            foreach (string pluggedName in pluggedNames)
            {
                if (!notPluggedNames.Contains(pluggedName))
                {
                    newNames.Add(pluggedName);
                }
            }

            // Create a list of new devices based on the list of new device names
            List<ILiveDevice> newDevices = new List<ILiveDevice>();
            foreach (ILiveDevice newDevice in plugged)
            {
                if (newNames.Contains(newDevice.Name))
                {
                    newDevices.Add(newDevice);
                }
            }

            // If there's (strictly) one new device, assign it
            if (newDevices.Count == 1)
            {
                // Assign the new device
                pcapSelectedDevice = newDevices.First();

                // Remember the new device
                Properties.Settings.Default.pcapDevice = pcapSelectedDevice.Name;
                Properties.Settings.Default.Save();
            }
            else
            {
                // If there's more than one, don't auto-assign any of them
                if (newDevices.Count > 1)
                {
                    MessageBox.Show("Could not auto-assign; more than one new device was detected.", "Auto-Detect Receiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                // If there's no new ones, don't do anything
                else if (newDevices.Count == 0)
                {
                    MessageBox.Show("Could not auto-assign; no new devices were detected.", "Auto-Detect Receiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Refresh the dropdown
            PopulatePcapDropdown();
        }

        /// <summary>
        /// Event handler for AppDomain.CurrentDomain.UnhandledException.
        /// </summary>
        /// <remarks>
        /// Logs the exception info to a file and prompts the user with the exception message.
        /// </remarks>
        public static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            // The unhandled exception
            Exception unhandledException = args.ExceptionObject as Exception;

            // MessageBox message
            StringBuilder message = new StringBuilder();
            message.AppendLine("An unhandled error has occured:");
            message.AppendLine();
            message.AppendLine(unhandledException.GetFirstLine());
            message.AppendLine();

            // Create log if it hasn't been created yet
            Logging.CreateMainLog();
            // Use an alternate message if log couldn't be created
            if (Logging.MainLogExists)
            {
                // Log exception
                Logging.Main_WriteLine("-------------------");
                Logging.Main_WriteLine("UNHANDLED EXCEPTION");
                Logging.Main_WriteLine("-------------------");
                Logging.Main_WriteException(unhandledException);

                // Complete the message buffer
                message.AppendLine("A log of the error has been created, do you want to open it?");

                // Display message
                MessageBoxResult result = MessageBox.Show(message.ToString(), "Unhandled Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                // If user requested to, open the log
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(Logging.LogFolderPath);
                }
            }
            else
            {
                // Complete the message buffer
                message.AppendLine("An error log was unable to be created.");

                // Display message
                MessageBox.Show(message.ToString(), "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Close the log files
            Logging.CloseAll();
            // Save settings
            Properties.Settings.Default.Save();

            // Close program
            MessageBox.Show("The program will now shut down.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }
}
