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
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
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
        /// Default Pcap packet capture timeout in milliseconds.
        /// </summary>
        private const int DefaultPacketCaptureTimeoutMilliseconds = 50;

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
        private static bool packetCaptureActive = false;

        /// <summary>
        /// Flag indicating if packets should be shown.
        /// </summary>
        private static bool packetDebug = false;

        /// <summary>
        /// Flag indicating if packets should be logged to a file.
        /// </summary>
        private static bool packetDebugLog = false;

        /// <summary>
        /// Flag indicating that guitar 1 ID auto assigning is in progress.
        /// </summary>
        private static bool packetGuitar1AutoAssign = false;
        /// <summary>
        /// Flag indicating that guitar 2 ID auto assigning is in progress.
        /// </summary>
        private static bool packetGuitar2AutoAssign = false;
        /// <summary>
        /// Flag indicating that drum ID auto assigning is in progress.
        /// </summary>
        private static bool packetDrumAutoAssign = false;

        /// <summary>
        /// Common name for Pcap combo box items.
        /// </summary>
        private const string pcapComboBoxItemName = "pcapDeviceComboBoxItem";

        /// <summary>
        /// Common name for controller combo box items.
        /// </summary>
        private const string controllerComboBoxItemName = "controllerComboBoxItem";

        /// <summary>
        /// vJoy client.
        /// </summary>
        private static vJoy joystick;

        /// <summary>
        /// ViGEmBus client.
        /// </summary>
        private static ViGEmClient vigemClient = null;

        /// <summary>
        /// Index of the selected guitar 1 device.
        /// </summary>
        private static uint guitar1DeviceIndex = 0;

        /// <summary>
        /// Instrument ID for guitar 1.
        /// </summary>
        /// <remarks>
        /// An ID of 0 is assumed to be invalid.
        /// </remarks>
        private static ulong guitar1InstrumentId = 0;

        /// <summary>
        /// Index of the selected guitar 2 device.
        /// </summary>
        private static uint guitar2DeviceIndex = 0;

        /// <summary>
        /// Instrument ID for guitar 2.
        /// </summary>
        /// <remarks>
        /// An ID of 0 is assumed to be invalid.
        /// </remarks>
        private static ulong guitar2InstrumentId = 0;

        /// <summary>
        /// Index of the selected drum device.
        /// </summary>
        private static uint drumDeviceIndex = 0;

        /// <summary>
        /// Instrument ID for the drumkit.
        /// </summary>
        /// <remarks>
        /// An ID of 0 is assumed to be invalid.
        /// </remarks>
        private static ulong drumInstrumentId = 0;

        /// <summary>
        /// Counter for processed packets.
        /// </summary>
        private static ulong processedPacketCount = 0;

        /// <summary>
        /// Dictionary for ViGEmBus controllers.
        /// </summary>
        /// <remarks>
        /// uint = identifier for the instrument (1 for guitar 1, 2 for guitar 2, and 3 for drum)
        /// <br>IXbox360Controller = the controller associated with the instrument.</br>
        /// </remarks>
        private static Dictionary<uint,IXbox360Controller> vigemDictionary = new Dictionary<uint,IXbox360Controller>();

        /// <summary>
        /// Enumeration for ViGEmBus stuff.
        /// </summary>
        private enum VigemEnum
        {
            Guitar1 = 1,
            Guitar2 = 2,
            Drum = 3,
            DeviceIndex = 17
        }

        /// <summary>
        /// Initializes a new MainWindow.
        /// </summary>
        public MainWindow()
        {
            // Assign event handler for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            InitializeComponent();

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

            // Initialize dropdowns
            PopulatePcapDropdown();
            PopulateControllerDropdowns();
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
                Logging.LogLine("-------------------");
                Logging.LogLine("UNHANDLED EXCEPTION");
                Logging.LogLine("-------------------");
                Logging.LogException(unhandledException);

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

            // Close program
            MessageBox.Show("The program will now shut down.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
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

            // Dispose of the ViGEmBus client
            if (vigemClient != null)
            {
                vigemClient.Dispose();
            }

            // Close the log files
            Logging.CloseAll();
        }

        /// <summary>
        /// Acquires a vJoy device.
        /// </summary>
        /// <param name="joystick">The vJoy client to use.</param>
        /// <param name="deviceId">The device ID of the vJoy device to acquire.</param>
        /// <returns>True if device was successfully acquired, false otherwise.</returns>
        static bool AcquirevJoyDevice(vJoy joystick, uint deviceId)
        {
            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(deviceId);

            // Acquire the device
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(deviceId))))
            {
                Console.WriteLine($"Failed to acquire vJoy device number {deviceId}.");
                return false;
            }
            else
            {
                // Get the number of buttons 
                int nButtons = joystick.GetVJDButtonNumber(deviceId);

                Console.WriteLine($"Acquired vJoy device number {deviceId} with {nButtons} buttons.");
                return true;
            }
        }

        /// <summary>
        /// Creates a ViGEmBus device.
        /// </summary>
        /// <param name="joystick">The user index to index into the ViGEm dictionary.</param>
        /// <returns>True if device was successfully created or already exists, false otherwise.</returns>
        static bool CreateVigemDevice(uint userIndex)
        {
            // Don't add duplicate entries
            if (vigemDictionary.ContainsKey(userIndex))
            {
                // Returns true since it's already added
                return true;
            }

            IXbox360Controller vigemDevice = vigemClient.CreateXbox360Controller(0x1BAD, 0x0719); // Xbox 360 Rock Band wireless instrument vendor/product IDs
                // Rock Band Guitar: USB\VID_1BAD&PID_0719&IG_00  XUSB\TYPE_00\SUB_86\VEN_1BAD\REV_0002
                // Rock Band Drums:  USB\VID_1BAD&PID_0719&IG_02  XUSB\TYPE_00\SUB_88\VEN_1BAD\REV_0002
                // If subtype ID specification through ViGEmBus becomes possible at some point,
                // the guitar should be subtype 6, and the drums should be subtype 8

            // Register a temporary event handler for getting the user index
            // Prevents a race condition when getting the user index directly from the device
            vigemDevice.FeedbackReceived += OnVigemFeedbackReceived;

            try
            {
                // Throws one of 5 exceptions:
                // VigemBusNotFoundException
                // VigemTargetUninitializedException
                // VigemAlreadyConnectedException
                // VigemNoFreeSlotException
                // Win32Exception
                // These shouldn't happen in 99% of cases, catching just in case
                vigemDevice.Connect();
            }
            catch (Exception ex)
            {
                // Disconnect the device in case it was connected
                try { vigemDevice.Disconnect(); } catch {}

                // Log the exception
                Logging.LogLine("ViGEmBus device creation failed!");
                Logging.LogException(ex);

                // Create brief exception string
                string exceptionMessage = ex.GetFirstLine();
                string instrumentName = Enum.GetName(typeof(VigemEnum), userIndex);
                Console.WriteLine($"Could not create ViGEmBus device for {instrumentName}: {exceptionMessage}");

                return false;
            }

            vigemDictionary.Add(userIndex, vigemDevice);
            return true;
        }

        /// <summary>
        /// Temporary event handler for logging the user index of a ViGEm device.
        /// </summary>
        static void OnVigemFeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs args)
        {
            // Log the user index
            Console.WriteLine($"Created new ViGEmBus device with user index {args.LedNumber}");

            // Unregister the event handler
            (sender as IXbox360Controller).FeedbackReceived -= OnVigemFeedbackReceived;
        }

        /// <summary>
        /// Populates controller device selection combos.
        /// </summary>
        /// <remarks>
        /// Used both when initializing and when refreshing.
        /// </remarks>
        private void PopulateControllerDropdowns()
        {
            // Initialize the vJoy client
            joystick = new vJoy();

            // Check if vJoy is enabled
            bool vjoyFound = joystick.vJoyEnabled();
            if (!vjoyFound)
            {
                Console.WriteLine("No vJoy driver found, or vJoy is disabled. vJoy selections will be unavailable.");
            }
            else
            {
                // Log vJoy driver attributes (Vendor Name, Product Name, Version Number)
                Console.WriteLine("vJoy found! - Vendor: " + joystick.GetvJoyManufacturerString() + ", Product: " + joystick.GetvJoyProductString() + ", Version Number: " + joystick.GetvJoySerialNumberString());
            }

            // Check if ViGEmBus is installed
            bool vigemFound = false;
            if (vigemClient == null)
            {
                try
                {
                    vigemClient = new ViGEmClient();
                    vigemFound = true;
                    Console.WriteLine("ViGEmBus found!");
                }
                catch (Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException)
                {
                    vigemClient = null;
                    vigemFound = false;
                    Console.WriteLine("ViGEmBus not found. ViGEmBus selection will be unavailable.");
                }
            }
            else
            {
                vigemFound = true;
            }

            // Check if neither vJoy nor ViGEmBus were found
            if (!(vjoyFound || vigemFound))
            {
                MessageBox.Show("No controller emulators found! Please install either vJoy or ViGEmBus.\nThe program will now shut down.", "No Controller Emulators Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // Get default settings
            string currentGuitar1Selection = Properties.Settings.Default.currentGuitar1Selection;
            string currentGuitar2Selection = Properties.Settings.Default.currentGuitar2Selection;
            string currentDrumSelection = Properties.Settings.Default.currentDrumSelection;

            // Reset combo boxes
            guitar1Combo.Items.Clear();
            guitar2Combo.Items.Clear();
            drumCombo.Items.Clear();

            // Loop through vJoy IDs and populate dropdowns
            int freeDeviceCount = 0;
            for (uint id = 1; id <= 16; id++)
            {
                string vjoyDeviceName = $"vJoy Device {id}";
                string vjoyItemName = $"{controllerComboBoxItemName}{id}";
                bool isEnabled = false;

                // Get the state of the requested device
                if (vjoyFound)
                {
                    VjdStat status = joystick.GetVJDStatus(id);
                    switch (status)
                    {
                        case VjdStat.VJD_STAT_OWN:
                            vjoyDeviceName += " (device is already owned by this feeder)";
                            break;
                        case VjdStat.VJD_STAT_FREE:
                            int numButtons = joystick.GetVJDButtonNumber(id);
                            int numContPov = joystick.GetVJDContPovNumber(id);
                            bool xExists = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X); // X axis
                            bool yExists = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y); // Y axis
                            bool zExists = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z); // Z axis
                            // Check that vJoy device is configured correctly
                            if (numButtons >= 16 &&
                               numContPov >= 1 &&
                               xExists &&
                               yExists &&
                               zExists
                            )
                            {
                                isEnabled = true;
                                freeDeviceCount++;
                            }
                            else
                            {
                                vjoyDeviceName += " (device misconfigured, use 16 buttons, X/Y/Z axes, and 1 continuous POV)";
                            }
                            break;
                        case VjdStat.VJD_STAT_BUSY:
                            vjoyDeviceName += " (device is already owned by another feeder)";
                            break;
                        case VjdStat.VJD_STAT_MISS:
                            vjoyDeviceName += " (device is not installed or disabled)";
                            break;
                        default:
                            vjoyDeviceName += " (general error)";
                            break;
                    };
                }
                else
                {
                    vjoyDeviceName += " (vJoy disabled/not found)";
                }

                // Add combo item to combos
                // Guitar 1 combo
                ComboBoxItem vjoyComboBoxItem = new ComboBoxItem();
                vjoyComboBoxItem.Content = vjoyDeviceName;
                vjoyComboBoxItem.Name = vjoyItemName;
                vjoyComboBoxItem.IsEnabled = isEnabled;
                vjoyComboBoxItem.IsSelected = vjoyItemName.Equals(currentGuitar1Selection) && isEnabled;
                guitar1Combo.Items.Add(vjoyComboBoxItem);

                // Guitar 2 combo
                vjoyComboBoxItem = new ComboBoxItem();
                vjoyComboBoxItem.Content = vjoyDeviceName;
                vjoyComboBoxItem.Name = vjoyItemName;
                vjoyComboBoxItem.IsEnabled = isEnabled;
                vjoyComboBoxItem.IsSelected = vjoyItemName.Equals(currentGuitar2Selection) && isEnabled;
                guitar2Combo.Items.Add(vjoyComboBoxItem);

                // Drum combo
                vjoyComboBoxItem = new ComboBoxItem();
                vjoyComboBoxItem.Content = vjoyDeviceName;
                vjoyComboBoxItem.Name = vjoyItemName;
                vjoyComboBoxItem.IsEnabled = isEnabled;
                vjoyComboBoxItem.IsSelected = vjoyItemName.Equals(currentDrumSelection) && isEnabled;
                drumCombo.Items.Add(vjoyComboBoxItem);
            }

            if (vjoyFound)
            {
                Console.WriteLine($"Discovered {freeDeviceCount} free vJoy devices.");
            }

            // Create ViGEmBus device dropdown item
            string vigemDeviceName = $"ViGEmBus Device";
            if (!vigemFound)
            {
                vigemDeviceName += " (ViGEmBus not found)";
            }
            string vigemItemName = $"{controllerComboBoxItemName}17";

            // Add ViGEmBus combo item
            // Guitar 1 combo
            ComboBoxItem vigemComboBoxItem = new ComboBoxItem();
            vigemComboBoxItem.Content = vigemDeviceName;
            vigemComboBoxItem.Name = vigemItemName;
            vigemComboBoxItem.IsEnabled = vigemFound;
            vigemComboBoxItem.IsSelected = vigemItemName.Equals(currentGuitar1Selection) && vigemFound;
            guitar1Combo.Items.Add(vigemComboBoxItem);

            // Guitar 2 combo
            vigemComboBoxItem = new ComboBoxItem();
            vigemComboBoxItem.Content = vigemDeviceName;
            vigemComboBoxItem.Name = vigemItemName;
            vigemComboBoxItem.IsEnabled = vigemFound;
            vigemComboBoxItem.IsSelected = vigemItemName.Equals(currentGuitar2Selection) && vigemFound;
            guitar2Combo.Items.Add(vigemComboBoxItem);

            // Drum combo
            vigemComboBoxItem = new ComboBoxItem();
            vigemComboBoxItem.Content = vigemDeviceName;
            vigemComboBoxItem.Name = vigemItemName;
            vigemComboBoxItem.IsEnabled = vigemFound;
            vigemComboBoxItem.IsSelected = vigemItemName.Equals(currentDrumSelection) && vigemFound;
            drumCombo.Items.Add(vigemComboBoxItem);

            // Add None option
            // Guitar 1 combo
            string noneItemName = $"{controllerComboBoxItemName}0";
            ComboBoxItem noneComboBoxItem = new ComboBoxItem();
            noneComboBoxItem.Content = "None";
            noneComboBoxItem.Name = noneItemName;
            noneComboBoxItem.IsEnabled = true;
            noneComboBoxItem.IsSelected = noneItemName.Equals(currentGuitar1Selection) || string.IsNullOrEmpty(currentGuitar1Selection); // Default to this selection
            guitar1Combo.Items.Add(noneComboBoxItem);

            // Guitar 2 combo
            noneComboBoxItem = new ComboBoxItem();
            noneComboBoxItem.Content = "None";
            noneComboBoxItem.Name = noneItemName;
            noneComboBoxItem.IsEnabled = true;
            noneComboBoxItem.IsSelected = noneItemName.Equals(currentGuitar2Selection) || string.IsNullOrEmpty(currentGuitar2Selection); // Default to this selection
            guitar2Combo.Items.Add(noneComboBoxItem);

            // Drum combo
            noneComboBoxItem = new ComboBoxItem();
            noneComboBoxItem.Content = "None";
            noneComboBoxItem.Name = noneItemName;
            noneComboBoxItem.IsEnabled = true;
            noneComboBoxItem.IsSelected = noneItemName.Equals(currentDrumSelection) || string.IsNullOrEmpty(currentDrumSelection); // Default to this selection
            drumCombo.Items.Add(noneComboBoxItem);

            // Load default device IDs
            // Guitar 1
            string hexString = Properties.Settings.Default.currentGuitar1Id;
            // Limit ID to 12 characters, as they are 48 bits, not 64
            if (hexString.Length > 12)
            {
                Console.WriteLine("Attempted to load an invalid Guitar 1 instrument ID. The ID has been reset.");
                guitar1InstrumentId = 0;
            }
            else if (!ParsingHelpers.HexStringToUInt64(hexString, out guitar1InstrumentId))
            {
                if (string.IsNullOrEmpty(hexString))
                {
                    guitar1InstrumentId = 0;
                }
                else
                {
                    guitar1InstrumentId = 0;
                    Console.WriteLine("Attempted to load an invalid Guitar 1 instrument ID. The ID has been reset.");
                }
            }
            guitar1IdTextBox.Text = (guitar1InstrumentId == 0) ? string.Empty : hexString;
            
            // Guitar 2
            hexString = Properties.Settings.Default.currentGuitar2Id;
            // Limit ID to 12 characters, as they are 48 bits, not 64
            if (hexString.Length > 12)
            {
                Console.WriteLine("Attempted to load an invalid Guitar 2 instrument ID. The ID has been reset.");
                guitar1InstrumentId = 0;
            }
            else if (!ParsingHelpers.HexStringToUInt64(hexString, out guitar2InstrumentId))
            {
                if (string.IsNullOrEmpty(hexString))
                {
                    guitar2InstrumentId = 0;
                }
                else
                {
                    guitar2InstrumentId = 0;
                    Console.WriteLine("Attempted to load an invalid Guitar 2 instrument ID. The ID has been reset.");
                }
            }
            guitar2IdTextBox.Text = (guitar2InstrumentId == 0) ? string.Empty : hexString;

            // Drum
            hexString = Properties.Settings.Default.currentDrumId;
            // Limit ID to 12 characters, as they are 48 bits, not 64
            if (hexString.Length > 12)
            {
                Console.WriteLine("Attempted to load an invalid Drum instrument ID. The ID has been reset.");
                guitar1InstrumentId = 0;
            }
            else if (!ParsingHelpers.HexStringToUInt64(hexString, out drumInstrumentId))
            {
                if (string.IsNullOrEmpty(hexString))
                {
                    drumInstrumentId = 0;
                }
                else
                {
                    drumInstrumentId = 0;
                    Console.WriteLine("Attempted to load an invalid Drum instrument ID. The ID has been reset.");
                }
            }
            drumIdTextBox.Text = (drumInstrumentId == 0) ? string.Empty : hexString;
        }

        /// <summary>
        /// Populates the Pcap device combo.
        /// </summary>
        /// <remarks>
        /// Used both when initializing, and when refreshing.
        /// </remarks>
        private void PopulatePcapDropdown()
        {
            // Disable auto-detect ID buttons
            guitar1IdAutoDetectButton.IsEnabled = false;
            guitar2IdAutoDetectButton.IsEnabled = false;
            drumIdAutoDetectButton.IsEnabled = false;

            // Clear combo list
            pcapDeviceCombo.Items.Clear();

            // Retrieve the device list from the local machine
            try
            {
                pcapDeviceList = CaptureDeviceList.Instance;
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Could not retrieve list of Pcap interfaces.");
                return;
            }

            if (pcapDeviceList == null || pcapDeviceList.Count == 0)
            {
                Console.WriteLine("No Pcap interfaces found!");
                return;
            }

            // Get default settings
            string currentPcapSelection = Properties.Settings.Default.currentPcapSelection;

            // Populate combo and print the list
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pcapDeviceList.Count; i++)
            {
                ILiveDevice device = pcapDeviceList[i];
                sb.Clear();
                string itemNumber = $"{i + 1}";
                sb.Append($"{itemNumber}. ");
                if (device.Description != null)
                {
                    sb.Append(device.Description);
                }
                sb.Append($" ({device.Name})");

                string deviceName = sb.ToString();
                string itemName = pcapComboBoxItemName + itemNumber;
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Name = itemName;
                comboBoxItem.Content = deviceName;
                comboBoxItem.IsEnabled = true;                
                bool isSelected = device.Name.Equals(currentPcapSelection) || device.Name.Equals(pcapSelectedDevice?.Name);
                comboBoxItem.IsSelected = isSelected;
                if (isSelected)
                {
                    // Re-enable auto-detect ID buttons and assign internal device reference
                    guitar1IdAutoDetectButton.IsEnabled = true;
                    guitar2IdAutoDetectButton.IsEnabled = true;
                    drumIdAutoDetectButton.IsEnabled = true;
                    pcapSelectedDevice = device;
                }

                pcapDeviceCombo.Items.Add(comboBoxItem);
            }

            // Set selection to nothing if saved device not detected
            if (pcapSelectedDevice == null)
            {
                pcapDeviceCombo.SelectedIndex = -1;
            }

            // Preset debugging flags
            string currentPacketDebugState = Properties.Settings.Default.currentPacketDebugState;
            if (currentPacketDebugState == "true")
            {
                packetDebugCheckBox.IsChecked = true;
            }

            string currentPacketLogState = Properties.Settings.Default.currentPacketDebugLogState;
            if (currentPacketLogState == "true")
            {
                packetLogCheckBox.IsChecked = true;
            }

            Console.WriteLine($"Discovered {pcapDeviceList.Count} Pcap devices.");
        }

        /// <summary>
        /// Handles Pcap device selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected combo box item
            ComboBoxItem typeItem = (ComboBoxItem)pcapDeviceCombo.SelectedItem;
            // Attempting to use typeItem's properties while null will cause a NullReferenceException
            if (typeItem == null)
            {
                Properties.Settings.Default.currentPcapSelection = String.Empty;
                Properties.Settings.Default.Save();
                return;
            }
            string itemName = typeItem.Name;

            // Get index of selected Pcap device
            int pcapDeviceIndex = -1;
            if (int.TryParse(itemName.Substring(pcapComboBoxItemName.Length), out pcapDeviceIndex))
            {
                // Adjust index count (UI->Logical)
                pcapDeviceIndex -= 1;

                // Assign device
                pcapSelectedDevice = pcapDeviceList[pcapDeviceIndex];
                Console.WriteLine($"Selected Pcap device {pcapSelectedDevice.Description}");

                // Enable auto-detect ID buttons
                guitar1IdAutoDetectButton.IsEnabled = true;
                guitar2IdAutoDetectButton.IsEnabled = true;
                drumIdAutoDetectButton.IsEnabled = true;

                // Remember selected Pcap device's name
                Properties.Settings.Default.currentPcapSelection = pcapSelectedDevice.Name;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handles guitar 1 controller selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void guitar1Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only allow a device to be selected by one selection, unless it is the None or ViGEmBus Device selections
            if (guitar1Combo.SelectedIndex < (int)VigemEnum.DeviceIndex)
            {
                if (guitar1Combo.SelectedIndex == guitar2Combo.SelectedIndex)
                {
                    guitar2Combo.SelectedIndex = -1;
                }

                if (guitar1Combo.SelectedIndex == drumCombo.SelectedIndex)
                {
                    drumCombo.SelectedIndex = -1;
                }
            }

            // Get selected guitar device
            ComboBoxItem typeItem = (ComboBoxItem)guitar1Combo.SelectedItem;
            // Attempting to use typeItem's properties while null will cause a NullReferenceException
            if (typeItem == null)
            {
                Properties.Settings.Default.currentGuitar1Selection = String.Empty;
                Properties.Settings.Default.Save();
                return;
            }
            string itemName = typeItem.Name;

            // Get index of selected guitar device
            if (uint.TryParse(typeItem.Name.Substring(controllerComboBoxItemName.Length), out guitar1DeviceIndex))
            {
                // Remember selected guitar device
                Properties.Settings.Default.currentGuitar1Selection = itemName;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handles guitar 2 controller selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void guitar2Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only allow a device to be selected by one selection, unless it is the None or ViGEmBus Device selections
            if (guitar2Combo.SelectedIndex < (int)VigemEnum.DeviceIndex)
            {
                if (guitar2Combo.SelectedIndex == guitar1Combo.SelectedIndex)
                {
                    guitar1Combo.SelectedIndex = -1;
                }

                if (guitar2Combo.SelectedIndex == drumCombo.SelectedIndex)
                {
                    drumCombo.SelectedIndex = -1;
                }
            }

            // Get selected guitar device
            ComboBoxItem typeItem = (ComboBoxItem)guitar2Combo.SelectedItem;
            // Attempting to use typeItem's properties while null will cause a NullReferenceException
            if (typeItem == null)
            {
                Properties.Settings.Default.currentGuitar2Selection = String.Empty;
                Properties.Settings.Default.Save();
                return;
            }
            string itemName = typeItem.Name;

            // Get index of selected guitar device
            if (uint.TryParse(typeItem.Name.Substring(controllerComboBoxItemName.Length), out guitar2DeviceIndex))
            {
                // Remember selected guitar device
                Properties.Settings.Default.currentGuitar2Selection = itemName;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handles drum controller selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void drumCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only allow a device to be selected by one selection, unless it is the None or ViGEmBus Device selections
            if (drumCombo.SelectedIndex < (int)VigemEnum.DeviceIndex)
            {
                if (drumCombo.SelectedIndex == guitar1Combo.SelectedIndex)
                {
                    guitar1Combo.SelectedIndex = -1;
                }

                if (drumCombo.SelectedIndex == guitar2Combo.SelectedIndex)
                {
                    guitar2Combo.SelectedIndex = -1;
                }
            }

            // Get selected drum device
            ComboBoxItem typeItem = (ComboBoxItem)drumCombo.SelectedItem;
            // Attempting to use typeItem's properties while null will cause a NullReferenceException
            if (typeItem == null)
            {
                Properties.Settings.Default.currentDrumSelection = String.Empty;
                Properties.Settings.Default.Save();
                return;
            }
            string itemName = typeItem.Name;

            // Get index of selected drum device
            if (uint.TryParse(typeItem.Name.Substring(controllerComboBoxItemName.Length), out drumDeviceIndex))
            {
                // Remember selected drum device
                Properties.Settings.Default.currentDrumSelection = itemName;
                Properties.Settings.Default.Save();
            }
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

            // Retrieve the device list from the local machine
            var allDevices = CaptureDeviceList.Instance;

            // Check if the device is still present
            bool deviceStillPresent = false;
            foreach (var device in allDevices)
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

            // Disable window controls
            pcapDeviceCombo.IsEnabled = false;
            pcapAutoDetectButton.IsEnabled = false;
            pcapRefreshButton.IsEnabled = false;
            packetDebugCheckBox.IsEnabled = false;
            packetLogCheckBox.IsEnabled = false;

            guitar1Combo.IsEnabled = false;
            guitar1IdTextBox.IsEnabled = false;
            guitar1IdAutoDetectButton.IsEnabled = false;

            guitar2Combo.IsEnabled = false;
            guitar2IdTextBox.IsEnabled = false;
            guitar2IdAutoDetectButton.IsEnabled = false;

            drumCombo.IsEnabled = false;
            drumIdTextBox.IsEnabled = false;
            drumIdAutoDetectButton.IsEnabled = false;

            controllerRefreshButton.IsEnabled = false;
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

            // Initialize vJoy
            bool vjoyResult;
            if (joystick != null)
            {
                // Reset buttons and axis
                joystick.ResetAll();

                // Acquire vJoy devices
                if (guitar1DeviceIndex > 0 && guitar1DeviceIndex < (int)VigemEnum.DeviceIndex)
                {
                    vjoyResult = AcquirevJoyDevice(joystick, guitar1DeviceIndex);
                    if (!vjoyResult)
                    {
                        StopCapture();
                        return;
                    }
                }

                if (guitar2DeviceIndex > 0 && guitar2DeviceIndex < (int)VigemEnum.DeviceIndex)
                {
                    vjoyResult = AcquirevJoyDevice(joystick, guitar2DeviceIndex);
                    if (!vjoyResult)
                    {
                        StopCapture();
                        return;
                    }
                }

                if (drumDeviceIndex > 0 && drumDeviceIndex < (int)VigemEnum.DeviceIndex)
                {
                    vjoyResult = AcquirevJoyDevice(joystick, drumDeviceIndex);
                    if (!vjoyResult)
                    {
                        StopCapture();
                        return;
                    }
                }
            }

            // Initialize ViGEmBus devices
            bool vigemResult;
            if (vigemClient != null)
            {
                // Create ViGEmBus devices for each
                if (guitar1DeviceIndex == (int)VigemEnum.DeviceIndex)
                {
                    vigemResult = CreateVigemDevice((uint)VigemEnum.Guitar1);
                    if (!vigemResult)
                    {
                        StopCapture();
                        return;
                    }
                }

                if (guitar2DeviceIndex == (int)VigemEnum.DeviceIndex)
                {
                    vigemResult = CreateVigemDevice((uint)VigemEnum.Guitar2);
                    if (!vigemResult)
                    {
                        StopCapture();
                        return;
                    }
                }

                if (drumDeviceIndex == (int)VigemEnum.DeviceIndex)
                {
                    vigemResult = CreateVigemDevice((uint)VigemEnum.Drum);
                    if (!vigemResult)
                    {
                        StopCapture();
                        return;
                    }
                }
            }

            // Register handler for packet debugging/logging and status reporting
            pcapSelectedDevice.OnPacketArrival += OnPacketArrival;

            // Start capture
            PacketParser.StartCapture(pcapSelectedDevice);
            Console.WriteLine($"Listening on {pcapSelectedDevice.Description}...");
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

                if (packetDebugLog)
                {
                    Logging.LogPacket(packetLogString);
                }
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

            // Release drum device
            if (joystick != null && drumDeviceIndex > 0)
            {
                joystick.RelinquishVJD(drumDeviceIndex);
            }

            // Release guitar 1 device
            if (joystick != null && guitar1DeviceIndex > 0)
            {
                joystick.RelinquishVJD(guitar1DeviceIndex);
            }

            // Release guitar 2 device
            if (joystick != null && guitar2DeviceIndex > 0)
            {
                joystick.RelinquishVJD(guitar2DeviceIndex);
            }

            // Disconnect ViGEmBus controllers
            foreach (IXbox360Controller device in vigemDictionary.Values)
            {
                device?.Disconnect();
            }
            vigemDictionary.Clear();

            // Store whether or not the packet log was created
            bool doPacketLogMessage = Logging.PacketLogExists;
            // Close packet log file
            Logging.ClosePacketLog();
            
            // Disable packet capture active flag
            packetCaptureActive = false;

            // Enable window controls
            pcapDeviceCombo.IsEnabled = true;
            pcapAutoDetectButton.IsEnabled = true;
            pcapRefreshButton.IsEnabled = true;
            packetDebugCheckBox.IsEnabled = true;
            packetLogCheckBox.IsEnabled = true;

            guitar1Combo.IsEnabled = true;
            guitar1IdTextBox.IsEnabled = true;
            guitar1IdAutoDetectButton.IsEnabled = true;

            guitar2Combo.IsEnabled = true;
            guitar2IdTextBox.IsEnabled = true;
            guitar2IdAutoDetectButton.IsEnabled = true;

            drumCombo.IsEnabled = true;
            drumIdTextBox.IsEnabled = true;
            drumIdAutoDetectButton.IsEnabled = true;

            controllerRefreshButton.IsEnabled = true;
            startButton.Content = "Start";

            // Disable packet count display
            packetsProcessedLabel.Visibility = Visibility.Hidden;
            packetsProcessedCountLabel.Visibility = Visibility.Hidden;
            packetsProcessedCountLabel.Content = string.Empty;
            processedPacketCount = 0;

            Console.WriteLine("Stopped capture.");
            if (doPacketLogMessage)
            {
                Console.WriteLine($"Packet logs may be found in {Logging.PacketLogFolderPath}.");
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
            packetLogCheckBox.IsEnabled = true;
            packetDebugLog = packetLogCheckBox.IsChecked.GetValueOrDefault();

            // Remember selected packet debug state
            Properties.Settings.Default.currentPacketDebugState = "true";
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
            packetLogCheckBox.IsEnabled = false;
            packetDebugLog = false;

            // Remember selected packet debug state
            Properties.Settings.Default.currentPacketDebugState = "false";
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
            Properties.Settings.Default.currentPacketDebugLogState = "true";
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
            Properties.Settings.Default.currentPacketDebugLogState = "false";
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

        /// <summary>
        /// Handles the click of the controller Refresh button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void controllerRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Re-populate dropdowns
            PopulateControllerDropdowns();
        }

        /// <summary>
        /// Handles the guitar 1 instrument ID textbox having its text changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void guitar1IdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Reset assignment
            guitar1InstrumentId = 0;

            // Set new ID
            string hexString = guitar1IdTextBox.Text.ToUpperInvariant();
            // Limit ID to 12 characters, as they are 48 bits, not 64
            if (hexString.Length > 12)
            {
                Console.WriteLine("Hex ID must be 12 characters or less.");
                return;
            }

            ulong enteredId;
            if (ParsingHelpers.HexStringToUInt64(hexString, out enteredId))
            {
                if (enteredId == 0)
                {
                    // Clear ID
                    Console.WriteLine("Cleared Hex ID for Guitar 1.");
                    hexString = string.Empty;
                }
                else if (enteredId == guitar2InstrumentId)
                {
                    // Enforce unique guitar instrument ID
                    Console.WriteLine("Guitar 1 ID must be different from Guitar 2 ID.");
                    hexString = string.Empty;
                }
                else
                {
                    // Set ID
                    guitar1InstrumentId = enteredId;
                    Console.WriteLine($"Guitar 1 instrument Hex ID set to {hexString}.");
                }
            }
            else if (string.IsNullOrEmpty(hexString))
            {
                // Clear ID
                Console.WriteLine("Cleared Hex ID for Guitar 1.");
                hexString = string.Empty;
            }
            else
            {
                Console.WriteLine("Invalid Hex ID entered for Guitar 1.");
                hexString = string.Empty;
            }

            // Update UI
            uiDispatcher.Invoke(() =>
            {
                guitar1IdTextBox.Text = hexString;
            });

            // Remember guitar 1 ID
            Properties.Settings.Default.currentGuitar1Id = hexString;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Handles the guitar 2 instrument ID textbox having its text changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void guitar2IdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Reset assignment
            guitar2InstrumentId = 0;

            // Set new ID
            string hexString = guitar2IdTextBox.Text.ToUpperInvariant();
            // Limit ID to 12 characters, as they are 48 bits, not 64
            if (hexString.Length > 12)
            {
                Console.WriteLine("Hex ID must be 12 characters or less.");
                return;
            }

            ulong enteredId;
            if (ParsingHelpers.HexStringToUInt64(hexString, out enteredId))
            {
                if (enteredId == 0)
                {
                    // Clear ID
                    Console.WriteLine("Cleared Hex ID for Guitar 2.");
                    hexString = string.Empty;
                }
                else if (enteredId == guitar1InstrumentId)
                {
                    // Enforce unique guitar instrument ID
                    Console.WriteLine("Guitar 2 ID must be different from Guitar 1 ID.");
                    hexString = string.Empty;
                }
                else
                {
                    // Set ID
                    guitar2InstrumentId = enteredId;
                    Console.WriteLine($"Guitar 2 instrument Hex ID set to {hexString}.");
                }
            }
            else if (string.IsNullOrEmpty(hexString))
            {
                // Clear ID
                Console.WriteLine("Cleared Hex ID for Guitar 2.");
                hexString = string.Empty;
            }
            else
            {
                Console.WriteLine("Invalid Hex ID entered for Guitar 2.");
                hexString = string.Empty;
            }

            // Update UI
            uiDispatcher.Invoke(() =>
            {
                guitar2IdTextBox.Text = hexString;
            });

            // Remember guitar 2 ID
            Properties.Settings.Default.currentGuitar2Id = hexString;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Handles the drum instrument ID textbox having its text changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void drumIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Reset assignment
            drumInstrumentId = 0;

            // Set new ID
            string hexString = drumIdTextBox.Text.ToUpperInvariant();
            // Limit ID to 12 characters, as they are 48 bits, not 64
            if (hexString.Length > 12)
            {
                Console.WriteLine("Hex ID must be 12 characters or less.");
                return;
            }

            ulong enteredId;
            if (ParsingHelpers.HexStringToUInt64(hexString, out enteredId))
            {
                if (enteredId == 0)
                {
                    // Clear ID
                    Console.WriteLine("Cleared Hex ID for Drum.");
                    hexString = string.Empty;
                }
                else
                {
                    // Set ID
                    drumInstrumentId = enteredId;
                    Console.WriteLine($"Drum instrument Hex ID set to {hexString}.");
                }
            }
            else if (string.IsNullOrEmpty(hexString))
            {
                // Clear ID
                Console.WriteLine("Cleared Hex ID for Drum.");
                hexString = string.Empty;
            }
            else
            {
                Console.WriteLine("Invalid Hex ID entered for Drum.");
                hexString = string.Empty;
            }

            // Update UI
            uiDispatcher.Invoke(() =>
            {
                drumIdTextBox.Text = hexString;
            });

            // Remember drum ID
            Properties.Settings.Default.currentDrumId = hexString;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Handles the Pcap auto-detect button being clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapAutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt user to unplug their receiver
            if (MessageBox.Show("Unplug your receiver, then click OK.", "Auto-Detect Receiver", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                // Get the list of devices for when receiver is unplugged
                CaptureDeviceList notPlugged = null;
                try
                {
                    notPlugged = CaptureDeviceList.Instance;
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Could not auto-assign; an error occured.", "Auto-Detect Receiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Logging.LogLine("Error during auto-assignment:");
                    Logging.LogException(ex);
                    return;
                }

                // Prompt user to plug in their receiver
                if (MessageBox.Show("Now plug in your receiver, wait a bit for it to register, then click OK.\n(A 1-second delay will be taken after clicking OK to ensure that it registers.)", "Auto-Detect Receiver", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    // Wait for a moment before getting the new list, seems like clicking OK too quickly after plugging it in makes it not get registered
                    Thread.Sleep(1000);

                    // Get the list of devices for when receiver is plugged in
                    CaptureDeviceList plugged = null;
                    try
                    {
                        plugged = CaptureDeviceList.Instance;
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show("Could not auto-assign; an error occured.", "Auto-Detect Receiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Logging.LogLine("Error during auto-assignment:");
                        Logging.LogException(ex);
                        return;
                    }

                    // Check for devices in the new list that aren't in the initial list
                    // Have to check names specifically, because doing `notPlugged.Contains(newDevice)`
                    // always adds every device in the list, even if it's not new

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
                        Properties.Settings.Default.currentPcapSelection = pcapSelectedDevice.Name;
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
            }
        }

        /// <summary>
        /// Handles the guitar 1 ID auto-detect button being clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void guitar1IdAutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            // Set auto-assign flag
            packetGuitar1AutoAssign = true;

            // Auto-detect ID
            AutoDetectID();
        }

        /// <summary>
        /// Handles the guitar 2 ID auto-detect button being clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void guitar2IdAutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            // Set auto-assign flag
            packetGuitar2AutoAssign = true;

            // Auto-detect ID
            AutoDetectID();
        }

        /// <summary>
        /// Handles the drum ID auto-detect button being clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void drumIdAutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            // Set auto-assign flag
            packetDrumAutoAssign = true;

            // Auto-detect ID
            AutoDetectID();
        }

        /// <summary>
        /// Automatically detects the instrument ID of a given packet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AutoDetectID()
        {
            // Disable all controls and show the auto-assign instruction label
            uiDispatcher.Invoke(() =>
            {
                pcapDeviceCombo.IsEnabled = false;
                pcapAutoDetectButton.IsEnabled = false;
                pcapRefreshButton.IsEnabled = false;
                packetDebugCheckBox.IsEnabled = false;
                packetLogCheckBox.IsEnabled = false;

                guitar1Combo.IsEnabled = false;
                guitar1IdTextBox.IsEnabled = false;
                guitar1IdAutoDetectButton.IsEnabled = false;

                guitar2Combo.IsEnabled = false;
                guitar2IdTextBox.IsEnabled = false;
                guitar2IdAutoDetectButton.IsEnabled = false;

                drumCombo.IsEnabled = false;
                drumIdTextBox.IsEnabled = false;
                drumIdAutoDetectButton.IsEnabled = false;

                controllerRefreshButton.IsEnabled = false;
                controllerAutoAssignLabel.Visibility = Visibility.Visible;

                startButton.IsEnabled = false;
            });

            // Await the result of auto-assignment
            bool result = await Task.Run(Read_AutoDetectID);
            if (!result)
            {
                MessageBox.Show("Failed to auto-assign ID.", "Auto-Assign ID", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Re-enable all controls and hide the auto-assign instruction label
            uiDispatcher.Invoke(() =>
            {
                pcapDeviceCombo.IsEnabled = true;
                pcapAutoDetectButton.IsEnabled = true;
                pcapRefreshButton.IsEnabled = true;
                packetDebugCheckBox.IsEnabled = true;
                packetLogCheckBox.IsEnabled = true;

                guitar1Combo.IsEnabled = true;
                guitar1IdTextBox.IsEnabled = true;
                guitar1IdAutoDetectButton.IsEnabled = true;

                guitar2Combo.IsEnabled = true;
                guitar2IdTextBox.IsEnabled = true;
                guitar2IdAutoDetectButton.IsEnabled = true;

                drumCombo.IsEnabled = true;
                drumIdTextBox.IsEnabled = true;
                drumIdAutoDetectButton.IsEnabled = true;

                controllerRefreshButton.IsEnabled = true;
                controllerAutoAssignLabel.Visibility = Visibility.Hidden;

                startButton.IsEnabled = true;
            });

            // Disable auto-assignment flags
            packetGuitar1AutoAssign = false;
            packetGuitar2AutoAssign = false;
            packetDrumAutoAssign = false;
        }

        /// <summary>
        /// Task function for auto-detecting an instrument ID.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool Read_AutoDetectID()
        {
            // Assume failure
            bool result = false;

            // Check if a device has been selected
            if (pcapSelectedDevice == null)
            {
                Console.WriteLine("Please select a Pcap device from the Pcap dropdown.");
                return false;
            }

            // Retrieve the device list from the local machine
            var allDevices = CaptureDeviceList.Instance;

            // Check if the device is still present
            bool deviceStillPresent = false;
            foreach (ILiveDevice device in allDevices)
            {
                if (device.Name == pcapSelectedDevice.Name)
                {
                    deviceStillPresent = true;
                    break;
                }
            }

            if (!deviceStillPresent)
            {
                uiDispatcher.Invoke((Action)(() =>
                {
                    // Invalidate selected device
                    pcapSelectedDevice = null;
                    // Notify user
                    Console.WriteLine("Pcap device list has changed and the selected device is no longer present. Please re-select your device from the list and try again.");
                    // Force a refresh
                    PopulatePcapDropdown();
                }));
                return false;
            }

            // Open the device
            pcapSelectedDevice.Open(new DeviceConfiguration()
                {
                    Snaplen = 45, // small packets
                    Mode = DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, // promiscuous mode with maximum speed
                    ReadTimeout = DefaultPacketCaptureTimeoutMilliseconds // read timeout
                }
            );

            // Receive packet
            PacketCapture packet;
            int attempts = 6;
            while (attempts > 0)
            {
                var status = pcapSelectedDevice.GetNextPacket(out packet);
                if (status == GetPacketStatus.PacketRead)
                {
                    RawCapture raw = packet.GetPacket();
                    byte[] data = raw.Data;
                    // RawCapture cannot be null here, as an instance is always created in the GetPacket function
                    // if (raw == null)
                    // {
                    //     return;
                    // }

                    // Debugging (if enabled)
                    if (packetDebug)
                    {
                        string packetHexString = ParsingHelpers.ByteArrayToHexString(data);
                        Console.WriteLine(raw.Timeval.Date.ToString("yyyy-MM-dd hh:mm:ss.fff") + $" [{raw.PacketLength}] " + packetHexString);
                    }

                    // Get ID from packet as Hex string
                    string idString = null;
                    if (raw.PacketLength == 40 || raw.PacketLength == 36)
                    {
                        // String representation: AA BB CC DD EE FF
                        uint id = (uint)(
                            data[15] |         // FF
                            (data[14] << 8) |  // EE
                            (data[13] << 16) | // DD
                            (data[12] << 24) | // CC
                            (data[11] << 32) | // BB
                            (data[10] << 40)   // AA
                        );

                        idString = Convert.ToString(id, 16).ToUpperInvariant();
                    }

                    // Check assignment flags and packet length
                    if (packetGuitar1AutoAssign && data.Length == 40)
                    {
                        // Update UI (assigns instrument ID)
                        uiDispatcher.Invoke((Action)(() =>
                        {
                            guitar1IdTextBox.Text = idString;
                        }));

                        result = true;
                    }
                    else if (packetGuitar2AutoAssign && data.Length == 40)
                    {
                        // Update UI (assigns instrument ID)
                        uiDispatcher.Invoke((Action)(() =>
                        {
                            guitar2IdTextBox.Text = idString;
                        }));

                        result = true;
                    }
                    else if (packetDrumAutoAssign && data.Length == 36)
                    {
                        // Update UI (assigns instrument ID)
                        uiDispatcher.Invoke((Action)(() =>
                        {
                            drumIdTextBox.Text = idString;
                        }));

                        result = true;
                    }
                    break;
                }

                // Short pause before retry
                Thread.Sleep(333);
                attempts--;
            }
            
            // Close device
            pcapSelectedDevice.Close();

            return result;
        }
    }
}
