using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RB4InstrumentMapper.Mapping;
using RB4InstrumentMapper.Parsing;
using RB4InstrumentMapper.Properties;

namespace RB4InstrumentMapper
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Dispatcher to send changes to UI.
        /// </summary>
        private static Dispatcher uiDispatcher = null;

        /// <summary>
        /// Whether or not packet capture is active.
        /// </summary>
        private bool packetCaptureActive = false;

        /// <summary>
        /// Available controller emulation types.
        /// </summary>
        private enum ControllerType
        {
            None = -1,
            vJoy = 0,
            ViGEmBus = 1,
            RPCS3 = 2
        }

        public MainWindow()
        {
            InitializeComponent();

            var version = Assembly.GetEntryAssembly().GetName().Version;
            versionLabel.Content = $"v{version}";
#if DEBUG
            versionLabel.Content += " Debug";
#endif

            // Capture Dispatcher object for use in callbacks
            uiDispatcher = Dispatcher;
        }

        /// <summary>
        /// Called when the window loads.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to console
            var textboxConsole = new TextBoxWriter(messageConsole);
            Console.SetOut(textboxConsole);

            // Check for vJoy
            bool vjoyFound = vJoyInstance.Enabled;
            if (vjoyFound)
            {
                // Log vJoy driver attributes (Vendor Name, Product Name, Version Number)
                Console.WriteLine($"vJoy found! - Vendor: {vJoyInstance.Manufacturer}, Product: {vJoyInstance.Product}, Version Number: {vJoyInstance.SerialNumber}");

                if (vJoyInstance.GetAvailableDeviceCount() > 0)
                {
                    vjoyDeviceTypeOption.IsEnabled = true;
                }
                else
                {
                    Console.WriteLine("No vJoy devices found. vJoy selection will be unavailable.");
                    vjoyDeviceTypeOption.IsEnabled = false;
                    vjoyDeviceTypeOption.IsSelected = false;
                }
            }
            else
            {
                Console.WriteLine("No vJoy driver found, or vJoy is disabled. vJoy selection will be unavailable.");
                vjoyDeviceTypeOption.IsEnabled = false;
                vjoyDeviceTypeOption.IsSelected = false;
            }

            // Check for ViGEmBus
            bool vigemFound = ViGEmInstance.TryInitialize();
            if (vigemFound)
            {
                Console.WriteLine("ViGEmBus found!");
                vigemDeviceTypeOption.IsEnabled = true;
                rpcs3DeviceTypeOption.IsEnabled = true;
            }
            else
            {
                Console.WriteLine("ViGEmBus not found. ViGEmBus selection will be unavailable.");
                vigemDeviceTypeOption.IsEnabled = false;
                vigemDeviceTypeOption.IsSelected = false;
                rpcs3DeviceTypeOption.IsEnabled = false;
                rpcs3DeviceTypeOption.IsSelected = false;
            }

            // Exit if neither ViGEmBus nor vJoy are installed
            if (!vjoyFound && !vigemFound)
            {
                MessageBox.Show("No controller emulators found! Please install vJoy or ViGEmBus.\nThe program will now shut down.", "No Controller Emulators Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // Load backend settings
            // Done after initializing virtual controller clients
            LoadBackendSettings();

            // Initialize backends
            GameInputBackend.DeviceCountChanged += GameInputDeviceCountChanged;
            GameInputBackend.Initialize();
            SetGameInputInitialized(GameInputBackend.Initialized);

            WinUsbBackend.DeviceCountChanged += WinUsbDeviceCountChanged;
            WinUsbBackend.Initialize();

            // Auto-start capture if applicable
            if ((Program.AutoStart || Settings.Default.autoStart) && startButton.IsEnabled)
            {
                StartCapture();
            }
        }

        private void LoadBackendSettings()
        {
            SetDeviceType((ControllerType)Settings.Default.controllerDeviceType);
            MappingSettings.UseAccurateDrumMappings = Settings.Default.accurateDrumMaps;
        }

        /// <summary>
        /// Called when the window has closed.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Shut down
            if (packetCaptureActive)
            {
                StopCapture();
            }

            GameInputBackend.Uninitialize();
            GameInputBackend.DeviceCountChanged -= GameInputDeviceCountChanged;

            WinUsbBackend.Uninitialize();
            WinUsbBackend.DeviceCountChanged -= WinUsbDeviceCountChanged;

            // Clean up
            Settings.Default.Save();
            Logging.CloseAll();
            ViGEmInstance.Dispose();
        }

        private void GameInputDeviceCountChanged()
        {
            uiDispatcher.Invoke(() => gameInputDeviceCountLabel.Content = $"Count: {GameInputBackend.DeviceCount}");
        }

        private void WinUsbDeviceCountChanged()
        {
            uiDispatcher.Invoke(() => usbDeviceCountLabel.Content = $"Count: {WinUsbBackend.DeviceCount}");
        }

        /// <summary>
        /// Configures the Pcap device and controller devices, and starts packet capture.
        /// </summary>
        private async void StartCapture()
        {
            // Start capture in backends
            await WinUsbBackend.StartCapture();
            GameInputBackend.StartCapture();
            packetCaptureActive = true;

            // Set window controls
            usbConfigureDevicesButton.IsEnabled = false;

            controllerDeviceTypeCombo.IsEnabled = false;

            packetDebugCheckBox.IsEnabled = false;
            packetLogCheckBox.IsEnabled = false;

            settingsButton.IsEnabled = false;

            startButton.Content = "Stop";

            // Initialize packet log
            if (packetLogCheckBox.IsEnabled && (packetLogCheckBox.IsChecked ?? false))
            {
                if (!Logging.CreatePacketLog())
                {
                    // Remaining context for this message is inside of the log creation
                    Console.WriteLine("Disabled packet logging for this capture session.");
                }
            }
        }

        /// <summary>
        /// Stops packet capture/mapping and resets Pcap/controller objects.
        /// </summary>
        private async void StopCapture()
        {
            await WinUsbBackend.StopCapture();
            GameInputBackend.StopCapture();

            // Store whether or not the packet log was created
            bool doPacketLogMessage = Logging.PacketLogExists;
            // Close packet log file
            Logging.ClosePacketLog();

            // Disable packet capture active flag
            packetCaptureActive = false;

            // Set window controls
            usbConfigureDevicesButton.IsEnabled = true;

            packetDebugCheckBox.IsEnabled = true;
            packetLogCheckBox.IsEnabled = true;

            settingsButton.IsEnabled = true;

            controllerDeviceTypeCombo.IsEnabled = true;

            startButton.Content = "Start";

            // Force a refresh of the controller textbox
            controllerDeviceTypeCombo_SelectionChanged(null, null);

            Console.WriteLine("Stopped capture.");
            if (doPacketLogMessage)
            {
                Console.WriteLine($"Packet logs may be found in {Logging.PacketLogFolderPath}");
            }
        }

        private void SetGameInputInitialized(bool enabled)
        {
            gameInputDeviceCountLabel.IsEnabled = enabled;
            gameInputRefreshButton.Content = enabled ? "Refresh" : "Initialize";
        }

        private void SetDeviceType(ControllerType type)
        {
            int typeInt = (int)type;
            if (controllerDeviceTypeCombo.SelectedIndex != typeInt)
            {
                // Set device type selection to the correct thing
                // Setting this fires off the handler, so we need to return
                // and let the second call set things
                controllerDeviceTypeCombo.SelectedIndex = typeInt;
                return;
            }

            Settings.Default.controllerDeviceType = typeInt;

            switch (type)
            {
                case ControllerType.vJoy:
                    if (vjoyDeviceTypeOption.IsEnabled && vJoyInstance.GetAvailableDeviceCount() > 0)
                    {
                        MapperFactory.MapperMode = MappingMode.vJoy;
                    }
                    else
                    {
                        // Reset device type selection
                        // Setting this fires off the handler again, no extra handling is needed
                        MapperFactory.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                case ControllerType.ViGEmBus:
                    if (vigemDeviceTypeOption.IsEnabled && ViGEmInstance.Initialized)
                    {
                        MapperFactory.MapperMode = MappingMode.ViGEmBus;
                    }
                    else
                    {
                        // Reset device type selection
                        // Setting this fires off the handler again, no extra handling is needed
                        MapperFactory.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                case ControllerType.RPCS3:
                    if (rpcs3DeviceTypeOption.IsEnabled && ViGEmInstance.Initialized)
                    {
                        MapperFactory.MapperMode = MappingMode.RPCS3;
                    }
                    else
                    {
                        // Reset device type selection
                        // Setting this fires off the handler again, no extra handling is needed
                        MapperFactory.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                case ControllerType.None:
                    MapperFactory.MapperMode = MappingMode.NotSet;
                    break;

                default:
                    MapperFactory.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                    break;
            }

            // Enable start button if a mapping mode is set
            startButton.IsEnabled = MapperFactory.MapperMode != MappingMode.NotSet;
            controllerDeviceTypeLabel.FontWeight = !startButton.IsEnabled ? FontWeights.Bold : FontWeights.Normal;
        }

        /// <summary>
        /// Handles the click of the Start button.
        /// </summary>
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
        /// Handles the packet debug checkbox being checked/unchecked.
        /// </summary>
        private void packetDebugCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool enabled = packetDebugCheckBox.IsChecked.GetValueOrDefault();

            PacketLogging.LogPackets = enabled;
            packetLogCheckBox.IsEnabled = enabled;
        }

        /// <summary>
        /// Handles the verbose error checkbox being checked.
        /// </summary>
        private void verboseLogCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Logging.PrintVerbose = verboseLogCheckBox.IsChecked.GetValueOrDefault();
        }

        /// <summary>
        /// Handles the click of the GameInput Refresh button.
        /// </summary>
        private void gameInputRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameInputBackend.Initialized)
            {
                GameInputBackend.Refresh();
            }
            else
            {
                GameInputBackend.Initialize();
                SetGameInputInitialized(GameInputBackend.Initialized);
            }
        }

        /// <summary>
        /// Handles the controller type setting being changed.
        /// </summary>
        private void controllerDeviceTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetDeviceType((ControllerType)controllerDeviceTypeCombo.SelectedIndex);
        }

        /// <summary>
        /// Handles the click of the Settings button.
        /// </summary>
        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow();

            // Position relative to the top-right of the main window
            window.Left = this.Left + (this.Width - window.Width - 50);
            window.Top = this.Top + 50;

            window.ShowDialog();

            LoadBackendSettings();
        }

        /// <summary>
        /// Handles the click of the USB Configure Devices button.
        /// </summary>
        private void usbConfigureDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable GameInput to prevent weird issues
            // Otherwise, devices switched over aren't disconnected on the GameInput side,
            // and devices reverted aren't picked up unless it's plugged in before RB4IM starts.
            // Both require a restart of the PC or GameInput service to fix.
            GameInputBackend.Uninitialize();

            if (!Settings.Default.switchDriverDisclaimerShown)
            {
                MessageBox.Show(
                    "This driver switching process has proven itself to be unstable for some users. " +
                    "If you continue, and the procedure fails, your device may become stuck in a non-functional driver state that requires manual intervention to fix.\n\n" +
                    "You have been warned! Refer to the manual uninstallation instructions linked in this window if you run into trouble.",
                    "DISCLAIMER",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                Settings.Default.switchDriverDisclaimerShown = true;
            }

            var window = new UsbDeviceListWindow();
            window.ShowDialog();

            GameInputBackend.Initialize();
        }
    }
}
