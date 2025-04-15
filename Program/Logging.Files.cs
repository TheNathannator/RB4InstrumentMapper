using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace RB4InstrumentMapper
{
    public static partial class Logging
    {
        /// <summary>
        /// The file to log errors to.
        /// </summary>
        private static StreamWriter mainLog = null;
        private static readonly object mainLock = new object();

        /// <summary>
        /// Gets whether or not the main log exists.
        /// </summary>
        public static bool MainLogExists => mainLog != null;

        private static bool allowMainLogCreation = true;

        /// <summary>
        /// The current file to log packets to.
        /// </summary>
        private static StreamWriter packetLog = null;
        private static readonly object packetLock = new object();

        /// <summary>
        /// Gets whether or not a packet log exists.
        /// </summary>
        public static bool PacketLogExists => packetLog != null;

        /// <summary>
        /// The path to the folder to write logs to.
        /// </summary>
        /// <remarks>
        /// Currently %USERPROFILE%\Documents\RB4InstrumentMapper\Logs
        /// </remarks>
        public static readonly string LogFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RB4InstrumentMapper",
            "Logs"
        );

        /// <summary>
        /// The path to the folder to write packet logs to.
        /// </summary>
        /// <remarks>
        /// Currently %USERPROFILE%\Documents\RB4InstrumentMapper\PacketLogs
        /// </remarks>
        public static readonly string PacketLogFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RB4InstrumentMapper",
            "PacketLogs"
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
        /// Creates a file stream with a specific path.
        /// </summary>
        private static StreamWriter CreateFileStreamAtPath(string filePath)
        {
            try
            {
                // Create directory if it doesn't exist
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

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
            lock (mainLock)
            {
                if (!allowMainLogCreation || mainLog != null)
                    return true;

                mainLog = CreateFileStream(LogFolderPath);
                if (mainLog == null)
                {
                    // Log could not be created, don't allow creating it again to prevent console spam
                    allowMainLogCreation = false;
                    return false;
                }
            }

            Console.WriteLine("Created main log file.");
            return true;
        }

        /// <summary>
        /// Creates the main log file at the specified path.
        /// </summary>
        public static bool CreateMainLog(string logFilePath)
        {
            lock (mainLock)
            {
                if (!allowMainLogCreation || mainLog != null)
                    return true;

                mainLog = CreateFileStreamAtPath(logFilePath);
                if (mainLog == null)
                {
                    // Log could not be created, don't allow creating it again to prevent console spam
                    allowMainLogCreation = false;
                    return false;
                }
            }

            Console.WriteLine($"Created main log file at: {logFilePath}");
            return true;
        }

        /// <summary>
        /// Creates a packet log file.
        /// </summary>
        public static bool CreatePacketLog()
        {
            lock (packetLock)
            {
                if (packetLog != null)
                    return true;

                packetLog = CreateFileStream(PacketLogFolderPath);
                if (packetLog == null)
                    return false;
            }

            Console.WriteLine("Created packet log file.");
            return true;
        }

        /// <summary>
        /// Writes a line to the log file.
        /// </summary>
        public static void Main_WriteLine(string text)
        {
            // Create log file if it hasn't been made yet
            CreateMainLog();

            lock (mainLock)
            {
                mainLog?.WriteLine(GetMessageHeader(text));
            }
        }

        /// <summary>
        /// Writes an exception, and any context, to the log.
        /// </summary>
        public static void Main_WriteException(Exception ex, string context = null)
        {
            // Create log file if it hasn't been made yet
            CreateMainLog();

            lock (mainLock)
            {
                mainLog?.WriteException(ex, context);
            }
        }

        public static void Packet_WriteLine(string text)
        {
            // Don't create log file if it hasn't been made yet
            // Packet log should be created manually
            // CreatePacketLog();

            lock (packetLock)
            {
                packetLog?.WriteLine(text);
            }
        }

        /// <summary>
        /// Closes the main log file.
        /// </summary>
        public static void CloseMainLog()
        {
            lock (mainLock)
            {
                mainLog?.Close();
                mainLog = null;
            }
        }

        /// <summary>
        /// Closes the active packet log file.
        /// </summary>
        public static void ClosePacketLog()
        {
            lock (packetLock)
            {
                packetLog?.Close();
                packetLog = null;
            }
        }

        /// <summary>
        /// Closes all log files.
        /// </summary>
        public static void CloseAll()
        {
            CloseMainLog();
            ClosePacketLog();
        }

        /// <summary>
        /// Gets the first line of an exception.
        /// </summary>
        public static string GetFirstLine(this Exception ex)
        {
            if (ex == null)
                return "(null exception)";

            int newLine = ex.Message.IndexOfAny(new[] { '\r', '\n' });
            if (newLine != -1)
                return ex.Message.Substring(0, newLine);
            else
                return ex.Message;
        }

        /// <summary>
        /// Writes an exception + stack trace to a stream writer.
        /// </summary>
        public static void WriteException(this StreamWriter stream, Exception ex, string context = null)
        {
            if (context != null)
                stream.WriteLine(context);
            stream.WriteLine(ex);
            stream.WriteLine();
        }

        /// <summary>
        /// Gets a message header with a timestamp.
        /// </summary>
        private static string GetMessageHeader(string message)
        {
            return $"[{DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}] {message}";
        }
    }
}
