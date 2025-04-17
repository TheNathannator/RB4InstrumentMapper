using System;

using MappingMode = RB4InstrumentMapper.Core.Mapping.MappingMode;

namespace RB4InstrumentMapper.CLI
{
    /// <summary>
    /// The parsed representation of the CLI's available arguments.
    /// </summary>
    public class Arguments
    {
        private const string HelpOption = "--help";

        private const string ModeOption = "--mode";
        private const string AccurateDrumsOption = "--accurate-drums";

        private const string WaitForDevicesOption = "--wait-for-devices";
        private const string TimeoutOption = "--timeout";

        private const string VerboseOption = "--verbose";
        private const string LogFileOption = "--log-file";
        private const string LogPacketsOption = "--log-packets";

        /// <summary>
        /// The mapping mode to be used.
        /// </summary>
        /// <seealso cref="Core.BackendSettings.MapperMode"/>
        public MappingMode MappingMode = MappingMode.NotSet;

        /// <inheritdoc cref="Core.BackendSettings.UseAccurateDrumMappings"/>
        /// <seealso cref="Core.BackendSettings.UseAccurateDrumMappings"/>
        public bool HardwareAccurateDrums = false;

        /// <summary>
        /// The amount of time to wait for devices to be connected.
        /// Null means to not wait at all.
        /// </summary>
        public int? WaitForDevicesPeriod = null;

        /// <summary>
        /// The amount of time to run the program for.
        /// Null means to run indefinitely.
        /// </summary>
        public int? TimeoutPeriod = null;

        /// <summary>
        /// The path to use for the main log file.
        /// Null means to use the default location (<c>Documents\RB4InstrumentMapper\Logs\log_{yyyy-MM-dd_HH-mm-ss}.txt</c>).
        /// </summary>
        /// <seealso cref="Core.Logging.CreateMainLog"/>
        public string LogFilePath;

        /// <summary>
        /// The path to use for the packet log file.
        /// Null means to not log packets.
        /// </summary>
        /// <seealso cref="Core.Logging.CreatePacketLog"/>
        public string PacketLogFilePath;

        /// <summary>
        /// Whether to log verbose messages.
        /// </summary>
        /// <seealso cref="Core.Logging.PrintVerbose"/>
        public bool VerboseLogging;

        /// <summary>
        /// Attempts to parse the given command-line arguments.
        /// </summary>
        public static bool TryParse(string[] args, out Arguments parsed)
        {
            parsed = new Arguments();

            if (args.Length == 0)
            {
                PrintHelp();
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case HelpOption:
                    {
                        PrintHelp();
                        return false;
                    }
                    case ModeOption:
                    {
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Error: Value for " + ModeOption + " argument is missing.");
                            PrintHelp();
                            return false;
                        }

                        string modeStr = args[++i];
                        if (modeStr.Equals("vigem", StringComparison.OrdinalIgnoreCase) ||
                            modeStr.Equals("vigembus", StringComparison.OrdinalIgnoreCase))
                        {
                            parsed.MappingMode = MappingMode.ViGEmBus;
                        }
                        else if (modeStr.Equals("vjoy", StringComparison.OrdinalIgnoreCase))
                        {
                            parsed.MappingMode = MappingMode.vJoy;
                        }
                        else if (modeStr.Equals("rpcs3", StringComparison.OrdinalIgnoreCase))
                        {
                            parsed.MappingMode = MappingMode.RPCS3;
                        }
                        else if (modeStr.Equals("shadps4", StringComparison.OrdinalIgnoreCase))
                        {
                            parsed.MappingMode = MappingMode.shadPS4;
                        }
                        else
                        {
                            Console.WriteLine($"Error: Invalid mapping mode '{modeStr}'");
                            PrintHelp();
                            return false;
                        }

                        break;
                    }
                    case AccurateDrumsOption:
                    {
                        parsed.HardwareAccurateDrums = true;
                        break;
                    }
                    case WaitForDevicesOption:
                    {
                        const int defaultDeviceWaitPeriod = 30;

                        parsed.WaitForDevicesPeriod = defaultDeviceWaitPeriod;

                        // Optional timeout value
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedTimeout))
                        {
                            if (parsedTimeout < 0)
                            {
                                Console.WriteLine("Error: Invalid wait timeout value. Please provide a positive integer.");
                                return false;
                            }

                            parsed.WaitForDevicesPeriod = parsedTimeout;
                            i++;
                        }

                        break;
                    }
                    case TimeoutOption:
                    {
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Error: Value for " + TimeoutOption + " argument is missing.");
                            PrintHelp();
                            return false;
                        }

                        if (!int.TryParse(args[++i], out int timeout) || timeout < 0)
                        {
                            Console.WriteLine("Error: Invalid timeout value. Please provide a positive integer.");
                            return false;
                        }

                        parsed.TimeoutPeriod = timeout;
                        break;
                    }
                    case VerboseOption:
                    {
                        parsed.VerboseLogging = true;
                        break;
                    }
                    case LogFileOption:
                    {
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Error: Value for " + LogFileOption + " argument is missing.");
                            PrintHelp();
                            return false;
                        }

                        parsed.LogFilePath = args[++i];
                        break;
                    }
                    case LogPacketsOption:
                    {
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Error: Value for " + LogPacketsOption + " argument is missing.");
                            PrintHelp();
                            return false;
                        }

                        parsed.PacketLogFilePath = args[++i];
                        break;
                    }
                    default:
                    {
                        Console.WriteLine($"Error: Unknown option '{arg}'");
                        PrintHelp();
                        return false;
                    }
                }
            }

            // Validate arguments
            if (parsed.MappingMode == MappingMode.NotSet)
            {
                Console.WriteLine("Error: Required argument --mode not found.");
                PrintHelp();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prints argument help information to the console.
        /// </summary>
        public static void PrintHelp()
        {
            Console.WriteLine($"RB4InstrumentMapper CLI v{Program.GetVersion()}");
            Console.WriteLine($"Usage: {Program.GetExecutableName()} [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --mode <mode>                      The mapping mode to use.");
            Console.WriteLine("                                     - mode: one of 'vigem'/'vigembus', 'vjoy', 'rpcs3', or 'shadps4', case insensitive.");
            Console.WriteLine("  --accurate-drums                   Use hardware-accurate drum mappings for ViGEmBus mode.");
            Console.WriteLine();
            Console.WriteLine("  --wait-for-devices [timeout]       Wait for devices to be detected before starting (default timeout: 30s).");
            Console.WriteLine("  --timeout <seconds>                Run for the specified number of seconds, and then exit.");
            Console.WriteLine();
            Console.WriteLine("  --verbose                          Enable verbose logging.");
            Console.WriteLine("  --log-file <path>                  Path to write logging output to.");
            Console.WriteLine("                                     (default: Documents\\RB4InstrumentMapper\\Logs\\log_{yyyy-MM-dd_HH-mm-ss}.txt)");
            Console.WriteLine("  --log-packets <path>               Log packets to the given file path.");
            Console.WriteLine();
            Console.WriteLine("  --help                             Display this help message.");
        }
    }
}
