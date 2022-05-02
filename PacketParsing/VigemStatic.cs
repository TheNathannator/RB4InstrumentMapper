using System;
using System.Collections.Generic;
using SharpPcap;
using SharpPcap.LibPcap;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Exceptions;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Static vJoy client.
    /// </summary>
    static class VigemStatic
    {
        /// <summary>
        /// Static ViGEmBus client.
        /// </summary>
        private static ViGEmClient client = null;

        /// <summary>
        /// Whether or not the ViGEmBus client has been initialized.
        /// </summary>
        public static bool Initialized
        {
            get => client != null;
        }

        /// <summary>
        /// Initializes the ViGEmBus client.
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                client = new ViGEmClient();
                return true;
            }
            catch
            {
                client = null;
                return false;
            }
        }

        /// <summary>
        /// Performs cleanup for the ViGEmBus client.
        /// </summary>
        public static void Close()
        {
            client?.Dispose();
            client = null;
        }

        /// <summary>
        /// Creates a new Xbox 360 device with the Xbox 360 Rock Band wireless instrument vendor/product IDs.
        /// </summary>
        public static IXbox360Controller CreateDevice() => client.CreateXbox360Controller(0x1BAD, 0x0719);
        // Rock Band Guitar: USB\VID_1BAD&PID_0719&IG_00  XUSB\TYPE_00\SUB_86\VEN_1BAD\REV_0002
        // Rock Band Drums:  USB\VID_1BAD&PID_0719&IG_02  XUSB\TYPE_00\SUB_88\VEN_1BAD\REV_0002
        // If subtype ID specification through ViGEmBus becomes possible at some point,
        // the guitar should be subtype 6, and the drums should be subtype 8
    }
}