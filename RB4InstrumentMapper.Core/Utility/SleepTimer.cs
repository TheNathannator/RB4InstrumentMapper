using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Threading;

namespace RB4InstrumentMapper.Core.Utility
{
    using static PInvoke;

    public unsafe class SleepTimer : IDisposable
    {
        private SafeFileHandle _timerHandle;

        public SleepTimer()
        {
            _timerHandle = CreateWaitableTimerEx(
                (SECURITY_ATTRIBUTES?)null,
                null,
                CREATE_WAITABLE_TIMER_HIGH_RESOLUTION,
                (uint)FILE_ACCESS_RIGHTS.DELETE |
                    (uint)FILE_ACCESS_RIGHTS.SYNCHRONIZE |
                    (uint)SYNCHRONIZATION_ACCESS_RIGHTS.TIMER_MODIFY_STATE
            );
            if (_timerHandle == null || _timerHandle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, "Failed to create thread sleep timer");
            }
        }

        public void Dispose()
        {
            _timerHandle?.Dispose();
            _timerHandle = null;
        }

        public void Sleep(double seconds)
        {
            if (_timerHandle == null || _timerHandle.IsInvalid)
            {
                throw new ObjectDisposedException(nameof(_timerHandle));
            }

            if (seconds < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            // This helps the actual wait time to be a little more accurate in testing
            seconds *= 0.95;

            long centiNanoSeconds = -(long)(seconds * 1000 * 1000 * 10);
            if (!SetWaitableTimer(_timerHandle, centiNanoSeconds, 0, null, null, false))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            var result = WaitForSingleObject(_timerHandle, INFINITE);
            if (result != WAIT_EVENT.WAIT_OBJECT_0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }
        }
    }
}
