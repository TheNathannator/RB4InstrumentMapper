using System;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Common interface for device mappers.
    /// </summary>
    interface IDeviceMapper
    {
        /// <summary>
        /// Parses an input packet.
        /// </summary>
        void ParseInput(ReadOnlySpan<byte> data, byte length, byte sequenceCount);

        /// <summary>
        /// Parses a virtual keycode packet.
        /// </summary>
        void ParseVirtualKey(ReadOnlySpan<byte> data, byte length, byte sequenceCount);

        /// <summary>
        /// Performs cleanup for the mapper.
        /// </summary>
        void Close();
    }
}