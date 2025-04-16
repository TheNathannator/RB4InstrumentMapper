using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RB4InstrumentMapper.Core;
using RB4InstrumentMapper.Core.Mapping;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.CLI
{
    public class Program
    {
        private const string ModeOption = "--mode";
        private const string TimeoutOption = "--timeout";
        private const string VerboseOption = "--verbose";
        private const string AccurateDrumsOption = "--accurate-drums";
        private const string HelpOption = "--help";
        private const string LogFileOption = "--log-file";
        private const string WaitForDevicesOption = "--wait-for-devices";

        private static bool verboseLogging = false;
        private static bool captureActive = false;
        private static int deviceCount = 0;
        private static string logFilePath = null;
        private static int timeout = 0;
        private static bool waitForDevices = false;
        private static int waitTimeout = 30; // Default 30 seconds to wait for devices

        public static int Main(string[] args)
        {
            // Register exception handler
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Process command line arguments
            MappingMode mappingMode = MappingMode.NotSet;
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                
                if (arg == HelpOption)
                {
                    PrintHelp();
                    return 0;
                }
                else if (arg == ModeOption && i + 1 < args.Length)
                {
                    string modeStr = args[++i];
                    if (modeStr.Equals("vigem", StringComparison.OrdinalIgnoreCase))
                    {
                        mappingMode = MappingMode.ViGEmBus;
                    }
                    else if (modeStr.Equals("vjoy", StringComparison.OrdinalIgnoreCase))
                    {
                        mappingMode = MappingMode.vJoy;
                    }
                    else if (modeStr.Equals("rpcs3", StringComparison.OrdinalIgnoreCase))
                    {
                        mappingMode = MappingMode.RPCS3;
                    }
                    else
                    {
                        Console.WriteLine($"Error: Invalid mapping mode '{modeStr}'");
                        PrintHelp();
                        return 1;
                    }
                }
                else if (arg == TimeoutOption && i + 1 < args.Length)
                {
                    if (!int.TryParse(args[++i], out timeout) || timeout < 0)
                    {
                        Console.WriteLine("Error: Invalid timeout value. Please provide a positive integer.");
                        return 1;
                    }
                }
                else if (arg == LogFileOption && i + 1 < args.Length)
                {
                    logFilePath = args[++i];
                }
                else if (arg == VerboseOption)
                {
                    verboseLogging = true;
                    Logging.PrintVerbose = true;
                }
                else if (arg == AccurateDrumsOption)
                {
                    BackendSettings.UseAccurateDrumMappings = true;
                }
                else if (arg == WaitForDevicesOption)
                {
                    waitForDevices = true;
                    
                    // Optional timeout value
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedTimeout) && parsedTimeout > 0)
                    {
                        waitTimeout = parsedTimeout;
                        i++;
                    }
                }
                else
                {
                    Console.WriteLine($"Error: Unknown option '{arg}'");
                    PrintHelp();
                    return 1;
                }
            }

            // Set up logging
            if (logFilePath != null)
            {
                Logging.CreateMainLog(logFilePath);
            }
            else
            {
                Logging.CreateMainLog();
            }

            Console.WriteLine($"RB4InstrumentMapper CLI Version {GetVersion()}");
            
            // Check if mapping mode is specified
            if (mappingMode == MappingMode.NotSet)
            {
                Console.WriteLine("Error: No mapping mode specified. Use --mode option.");
                PrintHelp();
                return 1;
            }

            // Set the mapping mode
            BackendSettings.MapperMode = mappingMode;
            Console.WriteLine($"Using mapping mode: {mappingMode}");

            try
            {
                // Initialize appropriate virtual controller driver
                bool driverInitialized = false;
                
                if (mappingMode == MappingMode.ViGEmBus || mappingMode == MappingMode.RPCS3)
                {
                    driverInitialized = ViGEmInstance.TryInitialize();
                    if (!driverInitialized)
                    {
                        Console.WriteLine("Error: Failed to initialize ViGEmBus driver.");
                        return 1;
                    }
                    Console.WriteLine("ViGEmBus driver initialized successfully.");
                }
                else if (mappingMode == MappingMode.vJoy)
                {
                    driverInitialized = vJoyInstance.Enabled;
                    if (!driverInitialized)
                    {
                        Console.WriteLine("Error: vJoy driver not found or disabled.");
                        return 1;
                    }
                    if (vJoyInstance.GetAvailableDeviceCount() <= 0)
                    {
                        Console.WriteLine("Error: No vJoy devices available.");
                        return 1;
                    }
                    Console.WriteLine("vJoy driver initialized successfully.");
                }

                if (!driverInitialized)
                {
                    Console.WriteLine("Error: No controller emulators found! Please install vJoy or ViGEmBus.");
                    return 1;
                }

                // Initialize backends
                GameInputBackend.DeviceCountChanged += OnDeviceCountChanged;
                GameInputBackend.Initialize();
                if (!GameInputBackend.Initialized)
                {
                    Console.WriteLine("Warning: GameInput backend failed to initialize.");
                }
                else
                {
                    Console.WriteLine("GameInput backend initialized successfully.");
                }

                WinUsbBackend.DeviceCountChanged += OnDeviceCountChanged;
                WinUsbBackend.Initialize();
                if (!WinUsbBackend.Initialized)
                {
                    Console.WriteLine("Warning: WinUSB backend failed to initialize.");
                }
                else
                {
                    Console.WriteLine("WinUSB backend initialized successfully.");
                }

                if (!GameInputBackend.Initialized && !WinUsbBackend.Initialized)
                {
                    Console.WriteLine("Error: All input backends failed to initialize.");
                    return 1;
                }

                UpdateDeviceCount();
                
                // Wait for devices if requested
                if (waitForDevices && deviceCount == 0)
                {
                    Console.WriteLine($"Waiting up to {waitTimeout} seconds for devices to be detected...");
                    int waitedSeconds = 0;
                    while (deviceCount == 0 && waitedSeconds < waitTimeout)
                    {
                        Thread.Sleep(1000);
                        waitedSeconds++;
                        
                        // Display progress every 5 seconds
                        if (waitedSeconds % 5 == 0)
                        {
                            Console.WriteLine($"Still waiting for devices... ({waitedSeconds}/{waitTimeout}s)");
                        }
                    }
                    
                    if (deviceCount == 0)
                    {
                        Console.WriteLine("No devices detected within timeout period.");
                        CleanupAndExit();
                        return 1;
                    }
                }

                // Start mapping
                Console.WriteLine("Starting instrument mapping...");
                StartCapture();

                // Set a timeout if specified
                CancellationTokenSource cts = null;
                if (timeout > 0)
                {
                    Console.WriteLine($"Will run for {timeout} seconds and then exit.");
                    cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                    
                    Task.Run(async () => {
                        await Task.Delay(TimeSpan.FromSeconds(timeout), cts.Token);
                        if (!cts.IsCancellationRequested)
                        {
                            Console.WriteLine("Timeout reached. Stopping...");
                            StopCapture();
                            Environment.Exit(0);
                        }
                    });
                }
                
                // Wait for Ctrl+C
                Console.WriteLine("Press Ctrl+C to stop mapping and exit.");
                Console.CancelKeyPress += (sender, eventArgs) => {
                    eventArgs.Cancel = true;
                    cts?.Cancel();
                    Console.WriteLine("Stopping instrument mapping...");
                    StopCapture();
                    Environment.Exit(0);
                };
                
                // Keep the program running
                Thread.Sleep(Timeout.Infinite);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (verboseLogging)
                {
                    Console.WriteLine(ex.ToString());
                }
                CleanupAndExit();
                return 1;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("RB4InstrumentMapper CLI");
            Console.WriteLine("Usage: RB4InstrumentMapperCLI [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --mode <mode>          Mapping mode: vigem, vjoy, or rpcs3");
            Console.WriteLine("  --timeout <seconds>    Run for the specified number of seconds and then exit");
            Console.WriteLine("  --log-file <path>      Path to write log file (default: auto-generated)");
            Console.WriteLine("  --verbose              Enable verbose logging");
            Console.WriteLine("  --accurate-drums       Use accurate drum mappings");
            Console.WriteLine("  --wait-for-devices [timeout] Wait for devices to be detected (default timeout: 30s)");
            Console.WriteLine("  --help                 Display this help message");
        }

        private static string GetVersion()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private static void OnDeviceCountChanged()
        {
            UpdateDeviceCount();
        }

        private static void UpdateDeviceCount()
        {
            int newDeviceCount = GameInputBackend.DeviceCount + WinUsbBackend.DeviceCount;
            
            if (newDeviceCount != deviceCount)
            {
                deviceCount = newDeviceCount;
                Console.WriteLine($"Devices detected: {deviceCount} (GameInput: {GameInputBackend.DeviceCount}, WinUSB: {WinUsbBackend.DeviceCount})");
            }
        }

        private static void StartCapture()
        {
            if (captureActive)
                return;

            Task.Run(async () => {
                await WinUsbBackend.StartCapture();
                GameInputBackend.StartCapture();
                captureActive = true;
                Console.WriteLine("Instrument mapping is active.");
            });
        }

        private static void StopCapture()
        {
            if (!captureActive)
                return;

            Task.Run(async () => {
                await WinUsbBackend.StopCapture();
                GameInputBackend.StopCapture();
                captureActive = false;
                Console.WriteLine("Instrument mapping stopped.");
            });
        }

        private static void CleanupAndExit()
        {
            if (captureActive)
            {
                StopCapture();
            }

            GameInputBackend.Uninitialize();
            GameInputBackend.DeviceCountChanged -= OnDeviceCountChanged;

            WinUsbBackend.Uninitialize();
            WinUsbBackend.DeviceCountChanged -= OnDeviceCountChanged;

            // Clean up
            Logging.CloseAll();
            ViGEmInstance.Dispose();
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var unhandledException = args.ExceptionObject as Exception;
            
            Console.WriteLine("An unhandled error has occurred:");
            Console.WriteLine(unhandledException?.Message ?? "Unknown error");
            
            if (verboseLogging && unhandledException != null)
            {
                Console.WriteLine(unhandledException.ToString());
            }
            
            if (Logging.MainLogExists)
            {
                Logging.Main_WriteLine("-------------------");
                Logging.Main_WriteLine("UNHANDLED EXCEPTION");
                Logging.Main_WriteLine("-------------------");
                Logging.Main_WriteException(unhandledException, "Unhandled exception!");
                Console.WriteLine($"Error log written to: {Logging.LogFolderPath}");
            }
            
            CleanupAndExit();
        }
    }
} 