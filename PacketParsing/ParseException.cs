using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Used to wrap or create exceptions that should be handled as part of parsing.
    /// </summary>
    class ParseException : Exception
    {
        public ParseException()
            : base() {}

        public ParseException(string message)
            : base(message) {}

        public ParseException(string message, Exception innerException)
            : base(message, innerException) {}

    }
}