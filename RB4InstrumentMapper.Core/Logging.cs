using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Core
{
    /// <summary>
    /// Provides functionality for logging.
    /// </summary>
    public static partial class Logging
    {
        public static bool PrintVerbose { get; set; } = false;

        public static void WriteLine(string message)
        {
            Debug.WriteLine(message);
            Main_WriteLine(message);
            Console.WriteLine(message);
        }

        public static void WriteLineVerbose(string message)
        {
            // Always log messages to debug/log
            Debug.WriteLine(message);
            Main_WriteLine(message);
            if (!PrintVerbose)
                return;

            Console.WriteLine(message);
        }

        public static void WriteException(string message, Exception ex)
        {
            Debug.WriteLine(message);
            Debug.WriteLine(ex);
            Main_WriteException(ex, message);
            Console.WriteLine(message);
            Console.WriteLine(ex.GetFirstLine());
        }

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