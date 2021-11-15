using PcapDotNet.Core;
using PcapDotNet.Packets;
using System;
using System.Collections.Generic;
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
        /// Default pcap packet capture timeout in milliseconds.
        /// </summary>
        private const int DefaultPacketCaptureTimeoutMilliseconds = 50;

        /// <summary>
        /// Selected pCap device.
        /// </summary>
        private int pcapDeviceIndex = -1;

        /// <summary>
        /// Communicator object that continously reads the WinPcap of selected device.
        /// </summary>
        private PacketCommunicator pcapCommunicator;

        /// <summary>
        /// Thread that handles the capture
        /// </summary>
        private Thread pcapCaptureThread;

        /// <summary>
        /// Flag indicating if packets should be shown.
        /// </summary>
        private static bool packetDebug = false;

        /// <summary>
        /// Common name for pcap combo box items.
        /// </summary>
        private const string pcapComboBoxItemName = "pcapDeviceComboBoxItem";

        /// <summary>
        /// Common name for vjoy combo box items.
        /// </summary>
        private const string controllerComboBoxItemName = "controllerComboBoxItem";

        /// <summary>
        /// Common joystick object and a position structure.
        /// </summary>
        private static vJoy joystick;

        /// <summary>
        /// ViGEmBus client.
        /// </summary>
        private static ViGEmClient vigemClient = null;

        /// <summary>
        /// Selected guitar 1 device
        /// </summary>
        private static uint guitar1DeviceIndex = 0;

        /// <summary>
        /// Packet instrument ID for guitar 1 device
        /// </summary>
        private static uint guitar1InstrumentId = 0; // This assumes that an ID of 0x00000000 is invalid

        /// <summary>
        /// Analyzed packet for guitar 1 device
        /// </summary>
        private static GuitarPacket guitar1Packet = new GuitarPacket();

        /// <summary>
        /// Selected guitar 2 device
        /// </summary>
        private static uint guitar2DeviceIndex = 0;

        /// <summary>
        /// Packet instrument ID for guitar 2 device
        /// </summary>
        private static uint guitar2InstrumentId = 0; // This assumes that an ID of 0x00000000 is invalid

        /// <summary>
        /// Analyzed packet for guitar 2 device
        /// </summary>
        private static GuitarPacket guitar2Packet = new GuitarPacket();

        /// <summary>
        /// Selected drum device.
        /// </summary>
        private static uint drumDeviceIndex = 0;

        /// <summary>
        /// Packet instrument ID for drum device
        /// </summary>
        private static uint drumInstrumentId = 0; // This assumes that an ID of 0x00000000 is invalid

        /// <summary>
        /// Analyzed packet for drum device
        /// </summary>
        private static DrumPacket drumPacket = new DrumPacket();

        /// <summary>
        /// Dictionary for ViGEmBus controllers.
        /// </summary>
        /// <remarks>
        /// uint = identifier for the instrument (1 for guitar 1, 2 for guitar 2, and 3 for drum)
        /// <br>IXbox360Controller = the controller associated with the instrument.</br>
        /// </remarks>
        private static Dictionary<uint,IXbox360Controller> vigemDictionary = new Dictionary<uint,IXbox360Controller>();

        /// <summary>
        /// Enumeration for ViGEmBus controller dictionary keys.
        /// </summary>
        private enum VigemInstruments
        {
            Guitar1 = 1,
            Guitar2 = 2,
            Drum = 3
        }

        /// <summary>
        /// Main window handler
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Capture Dispatcher object for use in callback
            uiDispatcher = this.Dispatcher;
        }

        /// <summary>
        /// Startup program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to console
            TextBoxConsole.RedirectConsoleToTextBox(messageConsole, displayLinesInReverseOrder: false);

            // Initialize dropdowns
            PopulatePcapDropdown();
            PopulateControllerDropdowns();
        }

        /// <summary>
        /// Shutdown program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Shutdown
            StopCapture();

            // Dispose of the ViGEmBus client
            if (vigemClient != null)
            {
                vigemClient.Dispose();
            }
        }

        /// <summary>
        /// Acquire vJoy device
        /// </summary>
        /// <param name="joystick">The vJoy object</param>
        /// <param name="deviceId">The device Id to acquire</param>
        static void AcquirevJoyDevice(vJoy joystick, uint deviceId)
        {
            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(deviceId);

            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(deviceId))))
            {
                Console.WriteLine($"Failed to acquire vJoy device number {deviceId}.");
                return;
            }
            else
            {
                // Get the number of buttons 
                int nButtons = joystick.GetVJDButtonNumber(deviceId);

                Console.WriteLine($"Acquired: vJoy device number {deviceId} with {nButtons} buttons.");
            }
        }

        static void CreateViGEmDevice(uint userIndex)
        {
            // Don't add duplicate entries
            if(vigemDictionary.ContainsKey(userIndex))
            {
                return;
            }

            vigemDictionary.Add(
                userIndex,
                vigemClient.CreateXbox360Controller(0x1BAD, 0x0719) // Xbox 360 Rock Band wireless instrument vendor/product IDs
                // Rock Band Guitar: USB\VID_1BAD&PID_0719&IG_00  XUSB\TYPE_00\SUB_86\VEN_1BAD\REV_0002
                // Rock Band Drums:  USB\VID_1BAD&PID_0719&IG_02  XUSB\TYPE_00\SUB_88\VEN_1BAD\REV_0002
                // If subtype ID specification through ViGEmBus becomes possible at some point,
                // the guitar should be subtype 6, and the drums should be subtype 8
            );
        }

        /// <summary>
        /// Populate controller device selection combos.
        /// </summary>
        private void PopulateControllerDropdowns()
        {
            // Create one joystick object and a position structure.
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

            // Re-check if ViGEmBus is installed
            if (vigemClient == null)
            {
                try
                {
                    vigemClient = new ViGEmClient();
                }
                catch(Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException)
                {
                    vigemClient = null;
                }
            }

            // Check if ViGEmBus is found
            bool vigemFound = vigemClient != null ? true : false;
            if (!vigemFound)
            {
                Console.WriteLine("ViGEmBus not found. ViGEmBus selection will be unavailable.");
            }
            else
            {
                Console.WriteLine("ViGEmBus found!");
            }

            if (!vjoyFound && !vigemFound)
            {
                MessageBox.Show("No controller emulators found! Please install either vJoy or ViGEmBus.", "No Controller Emulators Found", MessageBoxButton.OK);
                return;
            }

            // Get default settings
            string currentGuitar1Selection = Properties.Settings.Default.currentGuitar1Selection;
            string currentGuitar2Selection = Properties.Settings.Default.currentGuitar2Selection;
            string currentDrumSelection = Properties.Settings.Default.currentDrumSelection;


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
                            isEnabled = true;
                            freeDeviceCount++;
                            break;
                        case VjdStat.VJD_STAT_BUSY:
                            vjoyDeviceName += " (device is already owned by this feeder)";
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

                // Guitar 1 combo item
                ComboBoxItem vjoyComboBoxItem = new ComboBoxItem();
                vjoyComboBoxItem.Content = vjoyDeviceName;
                vjoyComboBoxItem.Name = vjoyItemName;
                vjoyComboBoxItem.IsEnabled = isEnabled;
                vjoyComboBoxItem.IsSelected = vjoyItemName.Equals(currentGuitar1Selection) && isEnabled;
                vjoyGuitar1Combo.Items.Add(vjoyComboBoxItem);

                // Guitar 2 combo item
                vjoyComboBoxItem = new ComboBoxItem();
                vjoyComboBoxItem.Content = vjoyDeviceName;
                vjoyComboBoxItem.Name = vjoyItemName;
                vjoyComboBoxItem.IsEnabled = isEnabled;
                vjoyComboBoxItem.IsSelected = vjoyItemName.Equals(currentGuitar2Selection) && isEnabled;
                vjoyGuitar2Combo.Items.Add(vjoyComboBoxItem);

                // Drum combo item
                vjoyComboBoxItem = new ComboBoxItem();
                vjoyComboBoxItem.Content = vjoyDeviceName;
                vjoyComboBoxItem.Name = vjoyItemName;
                vjoyComboBoxItem.IsEnabled = isEnabled;
                vjoyComboBoxItem.IsSelected = vjoyItemName.Equals(currentDrumSelection) && isEnabled;
                vjoyDrumCombo.Items.Add(vjoyComboBoxItem);
            }

            Console.WriteLine($"Discovered {freeDeviceCount} free vJoy devices.");

            // Create ViGEmBus device dropdown item
            string vigemDeviceName = $"ViGEmBus Device";
            if (!vigemFound)
            {
                vigemDeviceName += " (ViGEmBus not found)";
            }
            string vigemItemName = $"{controllerComboBoxItemName}17";

            // Guitar 1 combo item
            ComboBoxItem vigemComboBoxItem = new ComboBoxItem();
            vigemComboBoxItem.Content = vigemDeviceName;
            vigemComboBoxItem.Name = vigemItemName;
            vigemComboBoxItem.IsEnabled = vigemFound;
            vigemComboBoxItem.IsSelected = vigemItemName.Equals(currentGuitar1Selection) && vigemFound;
            vjoyGuitar1Combo.Items.Add(vigemComboBoxItem);

            // Guitar 2 combo item
            vigemComboBoxItem = new ComboBoxItem();
            vigemComboBoxItem.Content = vigemDeviceName;
            vigemComboBoxItem.Name = vigemItemName;
            vigemComboBoxItem.IsEnabled = vigemFound;
            vigemComboBoxItem.IsSelected = vigemItemName.Equals(currentGuitar2Selection) && vigemFound;
            vjoyGuitar2Combo.Items.Add(vigemComboBoxItem);

            // Drum combo item
            vigemComboBoxItem = new ComboBoxItem();
            vigemComboBoxItem.Content = vigemDeviceName;
            vigemComboBoxItem.Name = vigemItemName;
            vigemComboBoxItem.IsEnabled = vigemFound;
            vigemComboBoxItem.IsSelected = vigemItemName.Equals(currentDrumSelection) && vigemFound;
            vjoyDrumCombo.Items.Add(vigemComboBoxItem);

            // Preset device IDs
            
            // Guitar 1
            string hexString = Properties.Settings.Default.currentGuitar1Id;
            // Have to check for String.Empty, otherwise ToUInt32 throws an ArgumentOutOfRangeException
            if (hexString != String.Empty)
            {
                guitar1InstrumentId = Convert.ToUInt32(hexString, 16);
            }
            else
            {
                guitar1InstrumentId = 0;
            }

            guitar1IdTextBox.Text = (guitar1InstrumentId == 0) ? string.Empty : hexString;
            
            // Guitar 2
            hexString = Properties.Settings.Default.currentGuitar2Id;
            if (hexString != String.Empty)
            {
                guitar2InstrumentId = Convert.ToUInt32(hexString, 16);
            }
            else
            {
                guitar2InstrumentId = 0;
            }
            guitar2IdTextBox.Text = (guitar2InstrumentId == 0) ? string.Empty : hexString;
            
            // Drum
            hexString = Properties.Settings.Default.currentDrumId;
            if (hexString != String.Empty)
            {
                drumInstrumentId = Convert.ToUInt32(hexString, 16);
            }
            else
            {
                drumInstrumentId = 0;
            }
            drumIdTextBox.Text = (drumInstrumentId == 0) ? string.Empty : hexString;
        }

        /// <summary>
        /// Populate WinPcap device combos.
        /// </summary>
        private void PopulatePcapDropdown()
        {
            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (allDevices.Count == 0)
            {
                Console.WriteLine("No WinPcap interfaces found! Make sure WinPcap is installed.");
                return;
            }

            // Get default settings
            string currentPcapSelection = Properties.Settings.Default.currentPcapSelection;

            // Populate combo and print the list
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < allDevices.Count; i++)
            {
                LivePacketDevice device = allDevices[i];
                sb.Clear();
                string itemNumber = $"{i + 1}";
                sb.Append($"{itemNumber}. {device.Name}");
                if (device.Description != null)
                {
                    sb.Append($" ({device.Description})");
                }

                string deviceName = sb.ToString();
                string itemName = pcapComboBoxItemName + itemNumber;
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Name = itemName;
                comboBoxItem.Content = deviceName;
                comboBoxItem.IsEnabled = true;
                comboBoxItem.IsSelected = itemName.Equals(currentPcapSelection);
                pcapDeviceCombo.Items.Add(comboBoxItem);
            }

            // Preset debugging flag
            string currentPacketDebugState = Properties.Settings.Default.currentPacketDebugState;
            if (currentPacketDebugState == "true")
            {
                packetDebugCheckBox.IsChecked = true;
            }

            Console.WriteLine($"Discovered {allDevices.Count} WinPcap devices.");
        }

        /// <summary>
        /// Handle pcap device selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pcapDeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get selected pcap device
            ComboBoxItem typeItem = (ComboBoxItem)pcapDeviceCombo.SelectedItem;
            // Attempting to use typeItem's properties while null will cause a NullReferenceException
            if (typeItem == null)
            {
                Properties.Settings.Default.currentPcapSelection = String.Empty;
                Properties.Settings.Default.Save();
                return;
            }
            string itemName = typeItem.Name;

            // Get index of selected pcap device
            if (int.TryParse(itemName.Substring(pcapComboBoxItemName.Length), out pcapDeviceIndex))
            {
                // Adjust index count (UI->Logical)
                pcapDeviceIndex -= 1;

                // Remember selected pcap device
                Properties.Settings.Default.currentPcapSelection = itemName;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Handle guitar selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vjoyGuitarCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only allow a device to be selected by one selection, unless it is the ViGEmBus device selection
            if (guitar1Combo.SelectedIndex != 16)
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
        /// Handle guitar 2 selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vjoyGuitar2Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only allow a device to be selected by one selection, unless it is the ViGEmBus device selection
            if (guitar2Combo.SelectedIndex != 16)
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
        /// Handle drum selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vjoyDrumCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only allow a device to be selected by one selection, unless it is the ViGEmBus device selection
            if (drumCombo.SelectedIndex != 16)
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
        /// Configure pcap device and start packet capture.
        /// </summary>
        /// <param name="deviceIndex">pcap device index</param>
        private void StartCapture(int deviceIndex)
        {
            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

            // Take the selected adapter
            PacketDevice selectedDevice = allDevices[deviceIndex];

            // Open the device
            pcapCommunicator =
                selectedDevice.Open(
                    45, // small packets
                    PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.MaximumResponsiveness, // promiscuous mode with maximum speed
                    DefaultPacketCaptureTimeoutMilliseconds); // read timeout
            // Read data
            pcapCaptureThread = new Thread(ReadContinously);
            pcapCaptureThread.Start();

            Console.WriteLine($"Listening on {selectedDevice.Description}...");
        }

        /// <summary>
        /// Continously read messages from pcap device while it is active.
        /// </summary>
        private void ReadContinously()
        {
            // start the capture
            pcapCommunicator.ReceivePackets(0, PacketHandler);
        }

        /// <summary>
        /// Callback function invoked by Pcap.Net for every incoming packet
        /// </summary>
        /// <param name="packet">The received packet</param>
        private void PacketHandler(Packet packet)
        {
            // Don't use null packets
            if (packet == null)
            {
                return;
            }

            // Debugging (if enabled)
            if (packetDebug)
            {
                string packetHexString = ParsingHelpers.ByteArrayToHexString(packet.Buffer);
                Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + $" [{packet.Length}] " + packetHexString);
            }

            // Map guitar 1 (if enabled)
            if (guitar1DeviceIndex > 0 && guitar1InstrumentId != 0)
            {
                // Instrument ID must be unique
                if (guitar1InstrumentId == guitar2InstrumentId)
                {
                    return;
                }

                if (GuitarPacketReader.AnalyzePacket(packet.Buffer, out guitar1Packet))
                {
                    // vJoy
                    if (guitar1DeviceIndex < 17 && joystick != null)
                    {
                        if (GuitarPacketVjoyMapper.MapPacket(guitar1Packet, joystick, guitar1DeviceIndex, guitar1InstrumentId))
                        {
                            // Used packet
                            return;
                        }
                    }
                    // ViGEmBus
                    else if (guitar1DeviceIndex == 17 && vigemClient != null)
                    {
                        if (GuitarPacketViGEmMapper.MapPacket(guitar1Packet, vigemDictionary[(uint)VigemInstruments.Guitar1], guitar1InstrumentId))
                        {
                            // Used packet
                            return;
                        }
                    }
                }
            }

            // Map guitar 2 (if enabled)
            if (guitar2DeviceIndex > 0 && guitar2InstrumentId != 0)
            {
                // Instrument ID must be unique
                if (guitar2InstrumentId == guitar1InstrumentId)
                {
                    return;
                }

                if (GuitarPacketReader.AnalyzePacket(packet.Buffer, out guitar2Packet))
                {
                    // vJoy
                    if (guitar2DeviceIndex < 17 && joystick != null)
                    {
                        if (GuitarPacketVjoyMapper.MapPacket(guitar2Packet, joystick, guitar2DeviceIndex, guitar2InstrumentId))
                        {
                            // Used packet
                            return;
                        }
                    }
                    // ViGEmBus
                    else if (guitar2DeviceIndex == 17 && vigemClient != null)
                    {
                        if (GuitarPacketViGEmMapper.MapPacket(guitar2Packet, vigemDictionary[(uint)VigemInstruments.Guitar2], guitar2InstrumentId))
                        {
                            // Used packet
                            return;
                        }
                    }
                }
            }

            // Map drum (if enabled)
            if (drumDeviceIndex > 0 && drumInstrumentId != 0)
            {
                if (DrumPacketReader.AnalyzePacket(packet.Buffer, out drumPacket))
                {
                    // vJoy
                    if (drumDeviceIndex < 17 && joystick != null)
                    {
                        if (DrumPacketVjoyMapper.MapPacket(drumPacket, joystick, drumDeviceIndex, drumInstrumentId))
                        {
                            // Used packet
                            return;
                        }
                    }
                    // ViGEmBus
                    else if (drumDeviceIndex == 17 && vigemClient != null)
                    {
                        if (DrumPacketViGEmMapper.MapPacket(drumPacket, vigemDictionary[(uint)VigemInstruments.Drum], drumInstrumentId))
                        {
                            // Used packet
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stop capture and mapping of packets
        /// </summary>
        private void StopCapture()
        {
            // Stop packet capture
            if (pcapCommunicator != null)
            {
                pcapCommunicator.Break();
                pcapCommunicator = null;
            }

            // Stop processing thread
            if (pcapCaptureThread != null)
            {
                pcapCaptureThread.Join();
                pcapCaptureThread = null;
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
            if (vigemDictionary.Count != 0)
            {
                for (uint i = 0; i < vigemDictionary.Count; i++)
                {
                    if (vigemDictionary.ContainsKey(i) && vigemDictionary[i] != null)
                    {
                        vigemDictionary[i].Disconnect();
                    }
                }
            }
            vigemDictionary.Clear();
        }

        /// <summary>
        /// Handle 'Start' button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            // Initialize vJoy
            if (joystick != null)
            {
                // Reset buttons and axis
                joystick.ResetAll();

                // Acquire vJoy devices
                if (guitar1DeviceIndex > 0 && guitar1DeviceIndex < 17)
                {
                    AcquirevJoyDevice(joystick, guitar1DeviceIndex);
                }

                if (guitar2DeviceIndex > 0 && guitar2DeviceIndex < 17)
                {
                    AcquirevJoyDevice(joystick, guitar2DeviceIndex);
                }

                if (drumDeviceIndex > 0 && drumDeviceIndex < 17)
                {
                    AcquirevJoyDevice(joystick, drumDeviceIndex);
                }
            }

            if (vigemClient != null)
            {
                // Create ViGEmBus devices for each
                if (guitar1DeviceIndex == 17)
                {
                    CreateViGEmDevice((uint)VigemInstruments.Guitar1);
                }

                if (guitar2DeviceIndex == 17)
                {
                    CreateViGEmDevice((uint)VigemInstruments.Guitar2);
                }

                if (drumDeviceIndex == 17)
                {
                    CreateViGEmDevice((uint)VigemInstruments.Drum);
                }
            }

            // Start capture
            StartCapture(pcapDeviceIndex);

            // Only allow to start once
            startButton.IsEnabled = false;
        }

        /// <summary>
        /// Handle packet debug checkbox: enable packet output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetDebugCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            packetDebug = true;

            // Remember selected pcap debugging state
            Properties.Settings.Default.currentPacketDebugState = "true";
            Properties.Settings.Default.Save();

        }

        /// <summary>
        /// Handle packet debug checkbox: disable packet output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packetDebugCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            packetDebug = false;

            // Remember selected pcap debugging state
            Properties.Settings.Default.currentPacketDebugState = "false";
            Properties.Settings.Default.Save();
        }

        private void guitar1IdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set new ID
            string hexString = guitar1IdTextBox.Text;
            if (string.IsNullOrEmpty(hexString))
            {
                // Clear ID
                Console.WriteLine("Cleared Hex ID for Guitar 1.");
                guitar1InstrumentId = 0;
                hexString = string.Empty;
            }
            else
            {
                // Parse input string
                uint enteredId = Convert.ToUInt32(hexString, 16);
                if (enteredId == 0)
                {
                    // Clear ID
                    Console.WriteLine("Cleared Hex ID for Guitar 1.");
                    guitar1InstrumentId = 0;
                    hexString = string.Empty;
                }
                else if (enteredId == guitar2InstrumentId)
                {
                    // Enforce unique guitar instrument ID
                    Console.WriteLine("Guitar 1 ID must be different from Guitar 2 ID.");
                    return;
                }
                else
                {
                    // Set ID
                    guitar1InstrumentId = enteredId;
                    Console.WriteLine($"Set Guitar 1 instrument ID to {enteredId}.");
                }
            }

            // Remember guitar 1 ID
            Properties.Settings.Default.currentGuitar1Id = hexString;
            Properties.Settings.Default.Save();
        }

        private void guitar2IdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set new ID
            string hexString = guitar2IdTextBox.Text;
            if (string.IsNullOrEmpty(hexString))
            {
                // Clear ID
                Console.WriteLine("Cleared Hex ID for Guitar 2.");
                guitar2InstrumentId = 0;
                hexString = string.Empty;
            }
            else
            {
                // Parse input string
                uint enteredId = Convert.ToUInt32(hexString, 16);
                if (enteredId == 0)
                {
                    // Clear ID
                    Console.WriteLine("Cleared Hex ID for Guitar 2.");
                    guitar2InstrumentId = 0;
                    hexString = string.Empty;
                }
                else if (enteredId == guitar1InstrumentId)
                {
                    // Enforce unique guitar instrument ID
                    Console.WriteLine("Guitar 2 ID must be different from Guitar 1 ID.");
                    return;
                }
                else
                {
                    // Set ID
                    guitar2InstrumentId = enteredId;
                    Console.WriteLine($"Set Guitar 2 instrument ID to {enteredId}.");
                }
            }

            // Remember guitar 2 ID
            Properties.Settings.Default.currentGuitar2Id = hexString;
            Properties.Settings.Default.Save();
        }

        private void drumIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Set new ID
            string hexString = drumIdTextBox.Text;
            if (string.IsNullOrEmpty(hexString))
            {
                // Clear ID
                Console.WriteLine("Cleared Hex ID for Drum.");
                hexString = string.Empty;
                drumInstrumentId = 0;
            }
            else
            {
                // Parse input string
                uint enteredId = Convert.ToUInt32(hexString, 16);
                if (enteredId == 0)
                {
                    // Clear ID
                    Console.WriteLine("Cleared Hex ID for Drum.");
                    hexString = string.Empty;
                    drumInstrumentId = 0;
                }
                else
                {
                    // Set ID
                    guitar1InstrumentId = enteredId;
                    Console.WriteLine($"Set Drum instrument ID to {enteredId}.");
                }
            }

            // Remember drum ID
            Properties.Settings.Default.currentDrumId = hexString;
            Properties.Settings.Default.Save();
        }
    }
}
