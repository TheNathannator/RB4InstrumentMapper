using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Core
{
    /// <summary>
    /// Provides functionality for logging.
    /// </summary>
    public static partial class Logging
    {
        /// <summary>
        /// Whether to print verbose messages to the console.
        /// (They will always be written to the log file.)
        /// </summary>
        public static bool PrintVerbose { get; set; } = false;

        /// <summary>
        /// Prints the given message to the log, debug console, and standard output console.
        /// </summary>
        public static void WriteLine(string message)
        {
            Debug.WriteLine(message);
            Main_WriteLine(message);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Prints the given message to the log and debug console,
        /// along with the standard output console if <see cref="PrintVerbose"/> is enabled.
        /// </summary>
        public static void WriteLineVerbose(string message)
        {
            // Always log messages to debug/log
            Debug.WriteLine(message);
            Main_WriteLine(message);
            if (!PrintVerbose)
                return;

            Console.WriteLine(message);
        }

        /// <summary>
        /// Prints the given exception and message to the log, debug console, and standard output console.
        /// </summary>
        public static void WriteException(string message, Exception ex)
        {
            Debug.WriteLine(message);
            Debug.WriteLine(ex);
            Main_WriteException(ex, message);
            Console.WriteLine(message);
            Console.WriteLine(ex.GetFirstLine());
        }

        /// <summary>
        /// Prints the given exception and message to the log and debug console,
        /// along with the standard output console if <see cref="PrintVerbose"/> is enabled.
        /// </summary>
        public static void WriteExceptionVerbose(string message, Exception ex)
        {
            // Always log errors to debug/log
            Debug.WriteLine(message);
            Debug.WriteLine(ex);
            Main_WriteException(ex, message);

            if (!PrintVerbose)
                return;

            Console.WriteLine(message);
            Console.WriteLine(ex.GetFirstLine());
        }
    }
}