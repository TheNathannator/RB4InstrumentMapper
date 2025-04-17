using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using RB4InstrumentMapper.Core;
using RB4InstrumentMapper.Core.Mapping;
using RB4InstrumentMapper.Core.Parsing;

namespace RB4InstrumentMapper.CLI
{
    public class Program
    {
        private static bool captureActive = false;
        private static int deviceCount = 0;

        public static int Main(string[] args)
        {
            // Register exception handler
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Read arguments
            if (!Arguments.TryParse(args, out var parsedArgs))
            {
                return 1;
            }

            // Set up arguments
            Logging.PrintVerbose = parsedArgs.VerboseLogging;

            if (!string.IsNullOrEmpty(parsedArgs.LogFilePath))
            {
                Logging.CreateMainLog(parsedArgs.LogFilePath);
            }

            if (!string.IsNullOrEmpty(parsedArgs.PacketLogFilePath))
            {
                Logging.CreatePacketLog(parsedArgs.PacketLogFilePath);
            }

            BackendSettings.MapperMode = parsedArgs.MappingMode;
            BackendSettings.UseAccurateDrumMappings = parsedArgs.HardwareAccurateDrums;

            // Initialize
            Logging.WriteLine($"RB4InstrumentMapper CLI Version {GetVersion()}");
            Logging.WriteLine($"Using mapping mode: {BackendSettings.MapperMode}");
            if (!Initialize())
            {
                return 1;
            }

            try
            {
                // Wait for devices if requested
                if (!WaitForDeviceConnect(parsedArgs.WaitForDevicesPeriod))
                {
                    return 1;
                }

                // Start mapping
                Logging.WriteLine("Starting instrument mapping...");
                StartCapture();

                // Intercept Ctrl+C
                bool keepGoing = true;
                // Only send this message to the console, doesn't make sense in logs
                Console.WriteLine("Press Ctrl+C to stop mapping and exit.");
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    keepGoing = false;
                };

                // Wait for Ctrl+C or timeout period
                var timer = Stopwatch.StartNew();
                while (keepGoing)
                {
                    if (timer.Elapsed.Seconds >= parsedArgs.TimeoutPeriod)
                    {
                        Logging.WriteLine("Program timeout reached, stopping.");
                        break;
                    }

                    Thread.Sleep(100);
                }

                Logging.WriteLine("Stopping instrument mapping...");
                StopCapture();
                return 0;
            }
            finally
            {
                Uninitialize();
            }
        }

        public static string GetVersion()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        public static string GetExecutableName()
        {
            string executablePath = Assembly.GetEntryAssembly().Location;
            return Path.GetFileName(executablePath);
        }

        private static bool WaitForDeviceConnect(int? timeoutPeriod)
        {
            if (!timeoutPeriod.HasValue)
            {
                return true;
            }

            if (deviceCount == 0)
            {
                int timeout = timeoutPeriod.Value;
                Logging.WriteLine($"Waiting up to {timeout} seconds for devices to be detected...");

                int lastSeenSeconds = -1;
                var waitTimer = Stopwatch.StartNew();
                while (deviceCount == 0 && waitTimer.Elapsed.Seconds < timeout)
                {
                    // Display elapsed time
                    if (waitTimer.Elapsed.Seconds != lastSeenSeconds)
                    {
                        Console.CursorLeft = 0;
                        Console.Write($"{waitTimer.Elapsed.Seconds}/{timeout}s");
                        lastSeenSeconds = waitTimer.Elapsed.Seconds;
                    }

                    Thread.Sleep(100);
                }

                // Clear elapsed time
                Console.CursorLeft = 0;
                Console.Write(new string(' ', 40));
                Console.CursorLeft = 0;

                if (deviceCount == 0)
                {
                    Logging.WriteLine("No devices detected within timeout period, exiting.");
                    return false;
                }
            }

            Logging.WriteLine("Device found, starting mapping.");
            return true;
        }

        private static bool Initialize()
        {
            // Initialize the appropriate virtual controller driver
            if (BackendSettings.MapperMode == MappingMode.ViGEmBus || BackendSettings.MapperMode == MappingMode.RPCS3)
            {
                if (!ViGEmInstance.TryInitialize())
                {
                    Logging.WriteLine("Error: Failed to initialize ViGEmBus driver. Please ensure it is installed.");
                    return false;
                }

                Logging.WriteLine("ViGEmBus driver initialized successfully.");
            }
            else if (BackendSettings.MapperMode == MappingMode.vJoy)
            {
                if (!vJoyInstance.Enabled)
                {
                    Logging.WriteLine("Error: vJoy driver not found or disabled. Please ensure it is installed.");
                    return false;
                }

                if (vJoyInstance.GetAvailableDeviceCount() <= 0)
                {
                    Logging.WriteLine("Error: No vJoy devices available. Please ensure they are configured correctly.");
                    return false;
                }

                Logging.WriteLine("vJoy driver initialized successfully.");
            }

            // Initialize backends
            GameInputBackend.DeviceCountChanged += OnDeviceCountChanged;
            if (!GameInputBackend.Initialize())
            {
                Logging.WriteLine("Warning: GameInput backend failed to initialize.");
            }
            else
            {
                Logging.WriteLine("GameInput backend initialized successfully.");
            }

            WinUsbBackend.DeviceCountChanged += OnDeviceCountChanged;
            if (!WinUsbBackend.Initialize())
            {
                Logging.WriteLine("Warning: WinUSB backend failed to initialize.");
            }
            else
            {
                Logging.WriteLine("WinUSB backend initialized successfully.");
            }

            if (!GameInputBackend.Initialized && !WinUsbBackend.Initialized)
            {
                Logging.WriteLine("Error: All input backends failed to initialize.");
                return false;
            }

            UpdateDeviceCount();
            return true;
        }

        private static void Uninitialize()
        {
            StopCapture();

            GameInputBackend.Uninitialize();
            GameInputBackend.DeviceCountChanged -= OnDeviceCountChanged;

            WinUsbBackend.Uninitialize();
            WinUsbBackend.DeviceCountChanged -= OnDeviceCountChanged;

            // Clean up
            Logging.CloseAll();
            ViGEmInstance.Dispose();
        }

        private static void StartCapture()
        {
            if (captureActive)
                return;

            WinUsbBackend.StartCapture();
            GameInputBackend.StartCapture();
            captureActive = true;
            Logging.WriteLine("Instrument mapping is active.");
        }

        private static void StopCapture()
        {
            if (!captureActive)
                return;

            WinUsbBackend.StopCapture();
            GameInputBackend.StopCapture();
            captureActive = false;
            Logging.WriteLine("Instrument mapping stopped.");
        }

        private static void OnDeviceCountChanged()
        {
            UpdateDeviceCount();
        }

        private static void UpdateDeviceCount()
        {
            deviceCount = GameInputBackend.DeviceCount + WinUsbBackend.DeviceCount;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            // Build log message all at once to ensure no other messages cut into it
            var logMessage = new StringBuilder();
            logMessage.AppendLine("-------------------");
            logMessage.AppendLine("UNHANDLED EXCEPTION");
            logMessage.AppendLine("-------------------");
            logMessage.AppendLine(args.ExceptionObject?.ToString() ?? "(null error)");
            Logging.WriteLine(logMessage.ToString());

            // Only send this message to the console, doesn't make sense in logs
            Console.WriteLine($"An error log was written to {Logging.MainLogPath}");

            Uninitialize();
        }
    }
}