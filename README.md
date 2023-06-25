# RB4InstrumentMapper

A program that maps packets from Xbox One Rock Band 4 instruments to virtual controllers, for use in [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper Application Screenshot](/Docs/Images/ProgramScreenshot.png "RB4InstrumentMapper Application Screenshot")

Both guitars and drums are supported through an Xbox One wireless receiver.

## Software Requirements

- Windows 10 64-bit
- [WinPCap](https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe)
- [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest) or [vJoy](https://github.com/jshafer817/vJoy/releases/latest)

## Hardware Notes

Jaguar guitars require a firmware update in order to connect to the receiver.

- [Instructions](https://bit.ly/2UHzonU)
- [Firmware installer backup](https://drive.google.com/file/d/1DQxkkbBfi-UOqdX6vp5TaX6F2N2OBDra/view?usp=drivesdk)

Some guitars/drumkits might not sync properly when using just the sync button. This includes the PDP drumkit and occasionally the Jaguar guitar. Follow these steps to sync your device correctly:

1. Go to Windows settings > Devices > Bluetooth & other devices
2. Click `Add Bluetooth or other device` and pick the `Everything else` option.
3. Press and hold the sync button until the Xbox button light flashes quickly.
4. Select `Xbox compatible game controller` from the list once it appears.
5. If that doesn't work, restart your PC and try again.

## Installation

1. Install [WinPCap](https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe).
2. Install [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest) (recommended) or [vJoy](https://github.com/jshafer817/vJoy/releases/latest).
   - If you installed vJoy, configure it:
     1. Open your Start menu, find the `vJoy` folder, and open the `Configure vJoy` program inside it.
     2. Configure one device for each one of your guitars/drumkits, using these settings:
        - Number of Buttons: 16
        - POV Hat Switch: Continuous, POVs: 1
        - Axes: `X`, `Y`, `Z`

        ![vJoy Configuration Screenshot](/Docs/Images/vJoyConfiguration.png "vJoy Configuration Screenshot")

     3. Click Apply.
   - If you installed ViGEmBus, there's no configuration required. Outputs for guitars and drums will match that of their Xbox 360 counterparts.
3. Restart your PC.

## Usage

1. Select your Xbox One receiver from the dropdown menu.
   - Xbox receivers should be detected automatically.
   - If they are not, click the `Auto-Detect Pcap` button and follow its instructions.
2. Select either vJoy or ViGEmBus in the Controller Type dropdown.
3. Connect your instruments if you haven't yet.
4. Click the Start button. Devices will be detected automatically.
5. Map the controls for each instrument in Clone Hero:
   1. Press Space on the main menu.
   2. Click the Assign Controller button and press a button on the instrument for it to be assigned.
   3. Click the slots in the Controller column to map each of the controls.
   4. Repeat for each connected device.
   5. Click `Done`.

Selections are saved and should persist across program sessions.

## Packet Logs

RB4InstrumentMapper is capable of logging packets to a file for debugging purposes. To do so, enable both the `Show Packets (for debugging)` and `Log packets to file` checkboxes, then hit Start. Packet logs get saved to a `RB4InstrumentMapper` > `PacketLogs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for packet parsing issues.

Note that these settings are meant for debugging purposes only, leaving them enabled can reduce the performance of the program somewhat.

## Error Logs

In the case that the program crashes, an error log is saved to a `RB4InstrumentMapper` > `Logs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for the crash.

## References

- [GuitarSniffer repository](https://github.com/artman41/guitarsniffer)
- [DrumSniffer repository](https://github.com/Dunkalunk/guitarsniffer)

Packet Data:

- [GuitarSniffer guitar packet logs](https://1drv.ms/f/s!AgQGk0OeTMLwhA-uDO9IQHEHqGhv)
- GuitarSniffer guitar packet spreadsheets: [New](https://docs.google.com/spreadsheets/d/1ITZUvRniGpfS_HV_rBpSwlDdGukc3GC1CeOe7SavQBo/edit?usp=sharing), [Old](https://1drv.ms/x/s!AgQGk0OeTMLwg3GBDXFUC3Erj4Wb)

Additional documentation is available in the [PlasticBand documentation repository](https://github.com/TheNathannator/PlasticBand).

## Building

To build this program, you will need:

- Visual Studio (or MSBuild + your code editor of choice).
- [WiX Toolset](https://wixtoolset.org/) if you wish to build the installer.

## License

This program is licensed under the MIT license. See [LICENSE](LICENSE) for details.
