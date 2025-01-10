using System;

namespace RB4InstrumentMapper.Mapping
{
    /// <summary>
    /// A vJoy exception.
    /// </summary>
    class vJoyException : Exception
    {
        public vJoyException()
            : base() {}

        public vJoyException(string message)
            : base(message) {}

        public vJoyException(string message, Exception innerException)
            : base(message, innerException) {}

    }
}