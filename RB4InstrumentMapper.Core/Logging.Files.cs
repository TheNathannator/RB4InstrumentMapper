using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace RB4InstrumentMapper.Core
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
        /// The current path for the main log.
        /// </summary>
        public static string MainLogPath { get; private set; }

        /// <summary>
        /// The current path for the packet log.
        /// </summary>
        public static string PacketLogPath { get; private set; }

        /// <summary>
        /// Creates a log file path with the format of <c>log_{yyyy-MM-dd_HH-mm-ss}.txt</c>.
        /// </summary>
        private static string MakeDatedLogPath(string folderPath)
        {
            return Path.Combine(folderPath, $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
        }

        /// <summary>
        /// Creates a file stream at the specified path.
        /// </summary>
        private static StreamWriter CreateFileStream(string filePath)
        {
            try
            {
                // Create folder if it doesn't exist
                string folderPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                return new StreamWriter(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't create log file at {filePath}");
                Console.WriteLine(ex.GetFirstLine());
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Creates the main log file, optionally with the given file path.
        /// </summary>
        public static void CreateMainLog(string filePath = null)
        {
            lock (mainLock)
            {
                if (!allowMainLogCreation || mainLog != null)
                    return;

                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = MakeDatedLogPath(LogFolderPath);
                }

                mainLog = CreateFileStream(filePath);
                if (mainLog == null)
                {
                    // Log could not be created, don't allow creating it again to prevent console spam
                    allowMainLogCreation = false;
                    return;
                }

                MainLogPath = filePath;
            }

            Console.WriteLine($"Created main log file at {filePath}");
        }

        /// <summary>
        /// Creates a packet log file, optionally with the given file path.
        /// </summary>
        public static void CreatePacketLog(string filePath = null)
        {
            lock (packetLock)
            {
                if (packetLog != null)
                    return;

                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = MakeDatedLogPath(PacketLogFolderPath);
                }

                packetLog = CreateFileStream(filePath);
                if (packetLog == null)
                    return;

                PacketLogPath = filePath;
            }

            Console.WriteLine($"Created packet log file at {filePath}");
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
                MainLogPath = null;
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
                PacketLogPath = null;
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

            string message = ex.ToString();
            int newLine = message.IndexOfAny(new[] { '\r', '\n' });
            if (newLine != -1)
                return message.Substring(0, newLine);
            else
                return message;
        }

        /// <summary>
        /// Writes an exception + stack trace to a stream writer.
        /// </summary>
        public static void WriteException(this StreamWriter stream, Exception ex, string context = null)
        {
            stream.WriteLine(GetMessageHeader("EXCEPTION"));
            stream.WriteLine("------------------------------");
            // Prevent writing an empty line if context is not provided
            if (context != null)
                stream.WriteLine(context);
            stream.WriteLine(ex);
            stream.WriteLine("------------------------------");
        }

        /// <summary>
        /// Gets a message header with a timestamp.
        /// </summary>
        private static string GetMessageHeader(string message)
        {
            return $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        }
    }
}
