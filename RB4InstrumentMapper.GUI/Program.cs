using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RB4InstrumentMapper.Core;
using RB4InstrumentMapper.Core.Parsing;
using RB4InstrumentMapper.GUI.Properties;

namespace RB4InstrumentMapper.GUI
{
    public class Program
    {
        private const string AutoStartOption = "--autostart";
        private const string WinUsbOption = "--winusb";
        private const string RevertOption = "--revert";
        private const string ReplayOption = "--replay";

        public static bool AutoStart { get; private set; } = false;

        [STAThread]
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Regular app startup
            if (args.Length == 0)
            {
                App.Main();
                return 0;
            }

            int exitCode;
            switch (args[0])
            {
                case AutoStartOption:
                {
                    AutoStart = true;
                    App.Main();
                    return 0;
                }
                case WinUsbOption:
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Missing device instance ID argument");
                        return -1;
                    }

                    exitCode = WinUsbBackend.SwitchDeviceToWinUSB(args[1]) ? 0 : -1;
                    break;
                }
                case RevertOption:
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Missing device instance ID argument");
                        return -1;
                    }

                    exitCode = WinUsbBackend.RevertDevice(args[1]) ? 0 : -1;
                    break;
                }
                case ReplayOption:
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Missing log path argument");
                        return -1;
                    }

                    exitCode = ReplayBackend.ReplayLog(args[1]) ? 0 : -1;
                    break;
                }
                default:
                {
                    Console.WriteLine($"Invalid option '{args[0]}'");
                    exitCode = -1;
                    break;
                }
            }

            Logging.CloseAll();
            return exitCode;
        }

        public static Task<bool> StartWinUsbProcess(string instanceId)
        {
            string args = $"{WinUsbOption} {instanceId}";
            return RunElevated(args);
        }

        public static Task<bool> StartRevertProcess(string instanceId)
        {
            string args = $"{RevertOption} {instanceId}";
            return RunElevated(args);
        }

        private static async Task<bool> RunElevated(string args)
        {
            string location = Assembly.GetEntryAssembly().Location;
            var processInfo = new ProcessStartInfo()
            {
                Verb = "runas", // Run as admin
                FileName = location,
                Arguments = args,
                CreateNoWindow = true,
            };

            try
            {
                var process = Process.Start(processInfo);
                await Task.Run(process.WaitForExit);
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to create elevated process!");
                Debug.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Logs unhandled exceptions to a file and prompts the user with the exception message.
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            // Log message; built all at once to ensure no other messages cut into it.
            var message = new StringBuilder();
            message.AppendLine("-------------------");
            message.AppendLine("UNHANDLED EXCEPTION");
            message.AppendLine("-------------------");
            message.AppendLine(args.ExceptionObject?.ToString() ?? "(null error)");
            // Note for the check below: this will implicitly create the log
            Logging.Main_WriteLine(message.ToString());

            // MessageBox message
            message.Clear();
            message.AppendLine("An unhandled error has occured:");
            message.AppendLine();
            if (args.ExceptionObject is Exception ex)
            {
                message.AppendLine(ex.GetFirstLine());
            }
            else
            {
                message.AppendLine(args.ExceptionObject?.ToString() ?? "(null error)");
            }
            message.AppendLine();

            // Use an alternate message if log couldn't be created
            if (Logging.MainLogExists)
            {
                // Complete the message buffer
                message.AppendLine("A log of the error has been created, do you want to open it?");

                // Display message
                var result = MessageBox.Show(message.ToString(), "Unhandled Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                // If user requested to, open the log
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("explorer.exe", $"/select,\"{Logging.MainLogPath}\"").Dispose();
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
            Settings.Default.Save();
        }
    }
}