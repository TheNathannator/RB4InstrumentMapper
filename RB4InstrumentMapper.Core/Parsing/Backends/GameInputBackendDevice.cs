using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using RB4InstrumentMapper.Core.Mapping;
using RB4InstrumentMapper.Core.Utility;
using SharpGameInput;

namespace RB4InstrumentMapper.Core.Parsing
{
    internal class GameInputBackendDevice : IDisposable, IBackendClient
    {
        private const int E_NOTIMPL = unchecked((int)0x80004001);

        private IGameInput gameInput;
        private IGameInputDevice device;

        private Thread readThread;
        private EventWaitHandle threadStop = new EventWaitHandle(false, EventResetMode.ManualReset);
        private volatile bool ioError = false;

        private byte[] lastReport = Array.Empty<byte>();
        private int lastReportLength = 0;

        private bool inputsEnabled = false;

        public ushort VendorId { get; }
        public ushort ProductId { get; }

        public bool MapGuideButton => false;

        public IGameInputDevice Device => device;

        public GameInputBackendDevice(IGameInput gameInput, IGameInputDevice device)
        {
            this.gameInput = gameInput.Duplicate();
            this.device = device.Duplicate();

            ref readonly var info = ref device.DeviceInfo;
            VendorId = info.vendorId;
            ProductId = info.productId;
        }

        // No finalizer, all resources are managed

        public void Dispose()
        {
            inputsEnabled = false;

            threadStop?.Set();
            readThread?.Join();
            readThread = null;

            threadStop?.Dispose();
            threadStop = null;

            device?.Dispose();
            device = null;

            gameInput?.Dispose();
            gameInput = null;
        }

        public void EnableInputs(bool enabled)
        {
            if (inputsEnabled == enabled)
                return;

            inputsEnabled = enabled;
            if (enabled)
            {
                threadStop.Reset();
                readThread = new Thread(ReadThread) { IsBackground = true };
                readThread.Start();
            }
            else
            {
                threadStop.Set();
                readThread.Join();
            }
        }

        private unsafe void ReadThread()
        {
            using var deviceMapper = MapperFactory.GetByHardwareIds(this);
            if (deviceMapper == null)
            {
                GameInputBackend.QueueForRemoval(this);
                return;
            }

            if (gameInput.RegisterReadingCallback(
                device, GameInputKind.RawDeviceReport, 0, null,
                (token, context, reading, hasOverrunOccurred) =>
                {
                    using (reading)
                    {
                        if (!HandleReading(reading, deviceMapper))
                            token.Stop();
                    }
                },
                out var readingToken, out int result
            ))
            {
                threadStop.WaitOne(Timeout.Infinite);
                readingToken.Unregister(1000000);
            }
            else
            {
                // RegisterReadingCallback is not implemented at the time of writing,
                // so we fall back to polling on failure
                if (result != E_NOTIMPL)
                    Logging.WriteLineVerbose($"Couldn't register reading callback, falling back to manual polling. Error result: 0x{result:X4}");
                ReadThreaded(deviceMapper);
            }
        }

        private unsafe void ReadThreaded(DeviceMapper mapper)
        {
            using var sleepTimer = new SleepTimer();

            // We unfortunately can't rely on timestamp to determine state change,
            // as guitar axis changes do not change the timestamp
            // ulong lastTimestamp = 0;

            while (!threadStop.WaitOne(0))
            {
                sleepTimer.Sleep(1 / (double) BackendSettings.PollingFrequency);

                int hResult = gameInput.GetCurrentReading(GameInputKind.RawDeviceReport, device, out var reading);
                if (hResult < 0)
                {
                    if (hResult == (int)GameInputResult.ReadingNotFound)
                        continue;

                    if (hResult != (int)GameInputResult.DeviceDisconnected)
                        Logging.WriteLineVerbose($"Failed to get current reading: 0x{hResult:X8}");
                    break;
                }

                using (reading)
                {
                    // // Ignore unchanged reports
                    // ulong timestamp = reading.GetTimestamp();
                    // if (lastTimestamp == timestamp)
                    //     continue;
                    // lastTimestamp = timestamp;

                    if (!HandleReading(reading, mapper))
                        break;
                }
            }
        }

        private unsafe bool HandleReading(LightIGameInputReading reading, DeviceMapper mapper)
        {
            if (!reading.GetRawReport(out var rawReport))
            {
                Logging.WriteLineVerbose("Could not get raw report!");
                return false;
            }

            using (rawReport)
            {
                const int maxStackSize = 64;

                byte reportId = (byte)rawReport.ReportInfo.id;
                int reportSize = (int)rawReport.GetRawDataSize();

                byte[] poolBuffer = null;
                Span<byte> data = reportSize > maxStackSize
                    ? (poolBuffer = ArrayPool<byte>.Shared.Rent(reportSize))
                    : stackalloc byte[maxStackSize];

                try
                {
                    fixed (byte* ptr = data)
                    {
                        int readSize = (int)rawReport.GetRawData((UIntPtr)data.Length, ptr);
                        data = data.Slice(0, Math.Min(readSize, data.Length));
                    }

                    // Compare with last report to determine if any inputs changed
                    // Necessary due to the timestamp not updating on guitar axis changes
                    if (data.SequenceEqual(lastReport.AsSpan(0, lastReportLength)))
                        return true;

                    if (lastReport.Length < data.Length)
                        lastReport = new byte[data.Length];
                    data.CopyTo(lastReport);
                    lastReportLength = data.Length;

                    PacketLogging.WritePacket(
                        new ReadOnlySpan<byte>(&reportId, sizeof(byte)),
                        data,
                        PacketDirection.In
                    );

                    var result = mapper.HandleMessage(reportId, data);
                    if (result == XboxResult.Disconnected)
                        return false;
                }
                finally
                {
                    if (poolBuffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(poolBuffer);
                    }
                }
            }

            return true;
        }

        public XboxResult SendMessage(XboxMessage message)
        {
            return SendMessage(message.Header, message.Bytes);
        }

        public XboxResult SendMessage(XboxCommandHeader header)
        {
            return SendMessage(header, Span<byte>.Empty);
        }

        public unsafe XboxResult SendMessage<T>(XboxCommandHeader header, ref T data)
            where T : unmanaged
        {
            // Create a byte buffer for the given data
            var writeBuffer = new Span<byte>(Unsafe.AsPointer(ref data), sizeof(T));
            return SendMessage(header, writeBuffer);
        }

        public unsafe XboxResult SendMessage(XboxCommandHeader header, Span<byte> data)
        {
            PacketLogging.WritePacket(
                new ReadOnlySpan<byte>(&header.CommandId, sizeof(byte)),
                data,
                PacketDirection.Out
            );

            if (ioError)
                return XboxResult.Disconnected;

            const int retryThreshold = 3;
            for (int tryCount = 0; tryCount < retryThreshold; tryCount++,
                Logging.WriteLineVerbose($"Error while sending report! (Attempt {tryCount})"))
            {
                int hResult = device.CreateRawDeviceReport(header.CommandId, GameInputRawDeviceReportKind.Output, out var report);
                if (hResult < 0)
                {
                    if (hResult == (int)GameInputResult.DeviceDisconnected)
                        return XboxResult.Disconnected;

                    Logging.WriteLineVerbose($"Failed to create raw report: 0x{hResult:X8}");
                    continue;
                }

                using (report)
                {
                    if (!data.IsEmpty)
                    {
                        fixed (byte* ptr = data)
                        {
                            if (!report.SetRawData((UIntPtr)data.Length, ptr))
                            {
                                Logging.WriteLineVerbose("Failed to set raw report data!");
                                continue;
                            }
                        }
                    }

                    hResult = device.SendRawDeviceOutput(report);
                    if (hResult < 0)
                    {
                        // This call is not implemented as of the time of writing,
                        // ignore and treat as success
                        if (hResult == E_NOTIMPL)
                            return XboxResult.Success;

                        if (hResult == (int)GameInputResult.DeviceDisconnected)
                            return XboxResult.Disconnected;

                        Logging.WriteLineVerbose($"Failed to send raw report: 0x{hResult:X8}");
                        continue;
                    }

                    return XboxResult.Success;
                }
            }

            ioError = true;
            return XboxResult.Disconnected;
        }
    }
}