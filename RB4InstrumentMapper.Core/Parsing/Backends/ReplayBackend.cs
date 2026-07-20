using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RB4InstrumentMapper.Core.Mapping;

namespace RB4InstrumentMapper.Core.Parsing
{
    /// <summary>
    /// Replays packet log files to help with debugging packet handling issues.
    /// </summary>
    /// <remarks>
    /// Since it's only really meant for debugging, this replayer has a number
    /// of limitations and is not meant for general use:<br/>
    /// - It can only handle one device at a time.
    /// - Arrival and descriptor messages must be present in the log.
    /// </remarks>
    public static class ReplayBackend
    {
        public static bool ReplayLog(string logPath)
        {
            if (!File.Exists(logPath))
            {
                Console.WriteLine($"File not found: {logPath}");
                return false;
            }

            MappingMode mappingMode;
            if (ViGEmInstance.TryInitialize())
            {
                mappingMode = MappingMode.ViGEmBus;
            }
            else if (vJoyInstance.Enabled)
            {
                mappingMode = MappingMode.vJoy;
            }
            else
            {
                Console.WriteLine("No controller emulators available! Please make sure ViGEmBus and/or vJoy is installed.");
                return false;
            }

            BackendSettings.MapperMode = mappingMode;
            Console.WriteLine($"Using mapping mode {mappingMode}");

            Console.WriteLine("Lines of the log will be read one-by-one, pausing after each one.");
            Console.WriteLine("Press any key after this pause to move on to the next.");
            Console.WriteLine("Press any key to begin...");
            WaitForKey();

            string[] lines = File.ReadAllLines(logPath);
            var previousSequence = new Dictionary<byte, Dictionary<byte, byte>>();
            using var device = new XboxDevice(BackendType.Replay);
            device.EnableInputs(true);
            foreach (string line in lines)
            {
                // Remove any comments
                int spanEnd = line.IndexOf("//");
                if (spanEnd < 0)
                    spanEnd = line.Length;

                var lineSpan = line.AsSpan().Slice(0, spanEnd).Trim();
                if (lineSpan.IsEmpty)
                    continue;

                if (!PacketLogging.TryParsePacket(lineSpan, out var headerBytes, out var data, out var direction) ||
                    !XboxCommandHeader.TryParse(headerBytes, out var header, out _))
                {
                    Console.WriteLine($"Couldn't parse line: {line}");
                    Debugger.Break();
                    break;
                }

                // Skip packets that were sent from us
                if (direction != PacketDirection.In)
                {
                    Console.WriteLine($"Skipping direction-out line: {line}");
                    continue;
                }

                // Ensure correct header data length (for GameInput)
                header.DataLength = data.Length;

                // Set proper sequence ID if unspecified (for GameInput)
                if (header.SequenceCount == 0)
                {
                    if (!previousSequence.TryGetValue(header.Client, out var clientSequence))
                    {
                        clientSequence = new Dictionary<byte, byte>();
                        previousSequence.Add(header.Client, clientSequence);
                    }

                    if (!clientSequence.TryGetValue(header.CommandId, out byte sequence) ||
                        sequence == 0xFF) // Sequence IDs of 0 are not valid
                        sequence = 0;

                    header.SequenceCount = ++sequence;
                    clientSequence[header.CommandId] = sequence;
                }

                Console.WriteLine($"About to process line: {line}");
                WaitForKey();

                var result = device.HandlePacket(header, data);
                if (result != XboxResult.Success)
                {
                    Console.WriteLine($"Error when processing line: {result}");
                    Debugger.Break();
                    break;
                }
            }

            Console.WriteLine($"Replay finished. Press any key to continue...");
            WaitForKey();

            return true;
        }

        private static bool _consoleAvailable = true;

        [DebuggerStepThrough] // to make debug breaks show in the caller instead
        private static void WaitForKey()
        {
            if (_consoleAvailable)
            {
                try
                {
                    // Flush any existing keypresses
                    while (Console.KeyAvailable)
                    {
                        Console.ReadKey(intercept: true);
                    }

                    Console.ReadKey(intercept: true);
                    return;
                }
                catch (InvalidOperationException)
                {
                    _consoleAvailable = false;
                }
            }

            Debugger.Break();
        }
    }
}