using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace RB4InstrumentMapper
{
    /// <summary>
    /// Provides functionality for logging.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// The file to log errors to.
        /// </summary>
        private static StreamWriter mainLog = null;

        /// <summary>
        /// Gets whether or not the main log exists.
        /// </summary>
        public static bool MainLogExists
        {
            get => mainLog != null;
        }

        private static bool allowMainLogCreation = true;

        /// <summary>
        /// The current file to log packets to.
        /// </summary>
        private static StreamWriter packetLog = null;

        /// <summary>
        /// Gets whether or not a packet log exists.
        /// </summary>
        public static bool PacketLogExists
        {
            get => packetLog != null;
        }

        /// <summary>
        /// The path to the folder to write logs to.
        /// </summary>
        /// <remarks>
        /// Currently %USERPROFILE%\Documents\RB4InstrumentMapper\Logs
        /// </remarks>
        public static readonly string LogFolderPath = Path.Combine(
            System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RB4InstrumentMapper\\Logs"
        );

        /// <summary>
        /// The path to the folder to write packet logs to.
        /// </summary>
        /// <remarks>
        /// Currently %USERPROFILE%\Documents\RB4InstrumentMapper\PacketLogs
        /// </remarks>
        public static readonly string PacketLogFolderPath = Path.Combine(
            System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RB4InstrumentMapper\\PacketLogs"
        );

        /// <summary>
        /// Creates a file stream in the specified folder.
        /// </summary>
        /// <param name="folderPath">
        /// The folder to create the file in.
        /// </param>
        private static StreamWriter CreateFileStream(string folderPath)
        {
            // Create logs folder if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string currentTimeString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            string filePath = Path.Combine(folderPath, $"log_{currentTimeString}.txt");

            try
            {
                return new StreamWriter(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't create log file {filePath}:");
                Console.WriteLine(ex.GetFirstLine());
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Creates the main log file.
        /// </summary>
        public static bool CreateMainLog()
        {
            if (allowMainLogCreation && mainLog == null)
            {
                mainLog = CreateFileStream(LogFolderPath);
                if (mainLog != null)
                {
                    Console.WriteLine("Created main log file.");
                    return true;
                }
                else
                {
                    // Log could not be created, don't allow creating it again to prevent console spam
                    allowMainLogCreation = false;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a packet log file.
        /// </summary>
        public static bool CreatePacketLog()
        {
            if (packetLog == null)
            {
                packetLog = CreateFileStream(PacketLogFolderPath);
                if (packetLog != null)
                {
                    Console.WriteLine("Created packet log file.");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Writes a line to the log file.
        /// </summary>
        public static void Main_WriteLine(string text)
        {
            // Create log file if it hasn't been made yet
            CreateMainLog();

            mainLog?.WriteLine(text);
        }

        /// <summary>
        /// Writes an exception, and any additonal info, to the log.
        /// </summary>
        public static void Main_WriteException(Exception ex, string addtlInfo = null)
        {
            // Create log file if it hasn't been made yet
            CreateMainLog();

            mainLog?.WriteException(ex, addtlInfo);
        }

        public static void Packet_WriteLine(string text)
        {
            // Don't create log file if it hasn't been made yet
            // Packet log should be created manually
            packetLog?.WriteLine(text);
        }

        public static void Packet_Write(string text)
        {
            // Don't create log file if it hasn't been made yet
            // Packet log should be created manually
            packetLog?.Write(text);
        }

        /// <summary>
        /// Closes the main log file.
        /// </summary>
        public static void CloseMainLog()
        {
            mainLog?.Close();
            mainLog = null;
        }

        /// <summary>
        /// Closes the active packet log file.
        /// </summary>
        public static void ClosePacketLog()
        {
            packetLog?.Close();
            packetLog = null;
        }

        /// <summary>
        /// Closes all log files.
        /// </summary>
        public static void CloseAll()
        {
            CloseMainLog();
            ClosePacketLog();
        }

        // Extension method for getting the first line of Exception.ToString(),
        // since Exception.Message doesn't include the exception type
        /// <summary>
        /// Gets the first line of the <see cref="Exception.ToString()"/> method, to include the exception type in the message.
        /// </summary>
        /// <param name="ex">
        /// The exception being extended with this method.
        /// </param>
        public static string GetFirstLine(this Exception ex)
        {
            return ex?.ToString().Split('\n')[0];
        }

        // Extension method for logging exceptions to streams in a customized manner
        /// <summary>
        /// Writes an exception to the stream in a particularly emphasized fashion.
        /// </summary>
        /// <param name="stream">
        /// The StreamWriter being extended with this method.
        /// </param>
        /// <param name="ex">
        /// The exception to log.
        /// </param>
        /// <param name="addtlInfo">
        /// Additional info to log after the stack trace.
        /// </param>
        public static void WriteException(this StreamWriter stream, Exception ex, string addtlInfo = null)
        {
            // Current date and time, formatted in Year/Month/Date Hour:Minute:Second with the invariant culture
            string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            // Log
            stream.WriteLine($"[{timestamp}] EXCEPTION:");
            stream.WriteLine("------------------------------");
            stream.WriteLine(ex.ToString());
            // Prevent writing an empty line if additional info is not provided
            if (addtlInfo != null)
            {
                stream.WriteLine(addtlInfo);
            }
            stream.WriteLine("------------------------------");
        }
    }
}
