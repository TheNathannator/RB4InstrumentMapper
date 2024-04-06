# RB4InstrumentMapper

A program that maps packets from Xbox One instrument peripherals to virtual controllers, for use in games such as [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper main menu](/Docs/Images/ProgramScreenshot.png "RB4InstrumentMapper main menu")

All Xbox One instruments are supported (RB4 guitars/drums, GHL guitar), along with the RB4 wireless legacy adapter.

## Table of Contents

- [Requirements](#requirements)
  - [Software](#software)
  - [Hardware](#hardware)
- [Installation](#installation)
  - [Wireless Devices (Pcap)](#wireless-devices-pcap)
  - [Wired Devices (USB)](#wired-devices-usb)
  - [Controller Emulation Driver](#controller-emulation-driver)
- [Usage](#usage)
  - [Wireless Devices (Pcap)](#wireless-devices-pcap-1)
  - [Wired Devices (USB)](#wired-devices-usb-1)
  - [Starting the program](#starting-the-program)
  - [Mapping your controls in Clone Hero](#mapping-your-controls-in-clone-hero)
- [Packet Logs](#packet-logs)
- [Error Logs](#error-logs)
- [References](#references)
- [Building](#building)
- [License](#license)

## Requirements

### Software

- Windows 10/11 64-bit
- [WinPCap](https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe) (only for Wireless Devices, see [Hardware](#hardware))
- [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest) or [vJoy](https://github.com/jshafer817/vJoy/releases/latest)

### Hardware

- Wireless: Xbox One wireless receiver
  - This is *not* the same as Xbox 360 wireless receivers! You must get an Xbox One (or just "Xbox") receiver, such as [this one](https://amzn.to/2W7qQbt). Third-party receivers are untested, and they will not be deliberately supported.
- Wired: Guitar Hero Live Xbox One wireless dongle.
- Wired: Rock Band 4 wireless legacy adapter.

## Installation

### Wireless Devices (Pcap)

*This section of setup is not required if you will only be using Wired Devices. Note that the Guitar Hero Live guitar is not considered a wireless device to RB4InstrumentMapper, as its dongle acts as a wired device. Skip to the [Wired Devices (USB)](#wired-devices-usb) section for setup.*

Rock Band 4 guitars and drumkits, connected through an Xbox One receiver.

Install [WinPCap](https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe). This will be used to capture packets from the Xbox One receiver.


### Wired Devices (USB)

*This section of setup is not required if you will only be using Wireless Devices.*

Rock Band 4 wireless legacy adapter, Guitar Hero Live guitar + GHL Xbox One dongle.

No additional software installation is needed for USB devices. However, you will need to install the WinUSB driver on each device you wish to use; covered later.

### Controller Emulation Driver

This is the part that allows RB4InstrumentMapper to send inputs based on the packets it receives. It is required for both Wired Devices and Wireless Devices.

*Both of these can be installed simultaneously if desired, however RB4InstrumentMapper will only use one of them at a time.*
#### Option 1: [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest)
  - Recommended, as it requires no configuration and is significantly easier to use. All device outputs will match those of their Xbox 360 counterparts.
#### Option 2: [vJoy](https://github.com/jshafer817/vJoy/releases/latest)
  - Supported as an alternative to ViGEmBus, in case of issues. Requires some setup:
  1. Download and install vJoy.
  2. After installing, open your Start menu, find the `vJoy` folder, and open the `Configure vJoy` program inside it.
  3. Configure one device for each one of your controllers, using these settings:
     - Number of Buttons: 16
     - POV Hat Switch: Continuous, POVs: 1
     - Axes: `X`, `Y`, `Z`

     ![vJoy Configuration Screenshot](/Docs/Images/vJoyConfiguration.png "vJoy Configuration Screenshot")

  4. Click Apply.

### Jaguar Guitar Firmware Update

Jaguar guitars require a firmware update in order to connect to Xbox One receivers.

- [Instructions](https://bit.ly/2UHzonU)
- [CH wiki re-host](https://wiki.clonehero.net/link/61), in case the above link goes down again

## Usage

### Wireless Devices (Pcap)

- Ensure the `Enable` checkbox under the Pcap group is checked.
- Select your Xbox One receiver from the dropdown menu.
  - Xbox receivers should be detected automatically. If they are not, click the `Auto-Detect Pcap` button and follow its instructions.
- **Do not connect your instruments yet!** Hit the Start button first, or else devices may not function correctly.
  - A best effort is made for devices connected before starting, but correct behavior cannot be ensured for them.
- After hitting Start, connect your instruments. They will be picked up and mapped automatically as long as the program is running.

Some guitars/drumkits might not sync properly when using just the sync button. This includes the PDP drumkit and occasionally the Jaguar guitar. Follow these steps to sync your device correctly:

1. Go to Windows settings > Devices > Bluetooth & other devices
2. Click `Add Bluetooth or other device` and pick the `Everything else` option.
3. Press and hold the sync button until the Xbox button light flashes quickly.
4. Select `Xbox compatible game controller` from the list once it appears.
5. If that doesn't work, restart your PC and try again.

### Wired Devices (USB)

You will need to install the WinUSB driver onto each device you wish to use. **RB4InstrumentMapper is capable of doing this directly, through the `Configure Devices` button on its main menu.** However, if you run into any issues, there is an [alternative method](#alternate-winusb-install-instructions) of installing this driver.

![Configure Devices example](/Docs/Images/ConfigureDevices.png "Configure Devices example")

- Ensure the `Enable` checkbox under the USB group is checked.
- **Ensure you have installed WinUSB on the devices you want to use! They will not be recognized otherwise!**
  - You can do this easily by hitting the `Configure Devices` button under the USB group.
- Devices will be detected automatically as they are connected/removed, though they will not be mapped until you hit Start.

### Starting the program

Select either ViGEmBus or vJoy in the Controller Type dropdown, then hit the Start button. Devices will be picked up and mapped automatically until you hit Stop or close the program.

### Mapping your controls in Clone Hero

*Guitar Hero Live guitars paired with the GHL Xbox One dongle should have the correct mappings by default. If the strum bar or the lower frets don't register during songs, RB4InstrumentMapper may not be working correctly, remapping your controls will not fix this.*

Now that the program is running, map your controls for each instrument in Clone Hero:

1. Press Space on the main menu.
2. Click Assign Controller and press a button on the device for it to be assigned.
3. Click the slots in the Controller column to map each of the controls.
4. Repeat for each connected device.
5. Click `Done`.

## Packet Logs

RB4InstrumentMapper is capable of logging packets to a file for debugging purposes. To do so, enable both the `Show Packets (for debugging)` and `Log packets to file` checkboxes, then hit Start. Packet logs get saved to a `RB4InstrumentMapper` > `PacketLogs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for packet parsing issues.

Note that these settings are meant for debugging purposes only, leaving them enabled can reduce the performance of the program somewhat.

## Error Logs

In the case that the program crashes, an error log is saved to a `RB4InstrumentMapper` > `Logs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for the crash.

## Alternate WinUSB Install Instructions

*Wireless Devices: **DO NOT INSTALL THE DRIVER ON YOUR XBOX ONE RECEIVER!** RB4InstrumentMapper is not capable of handling it in this state, and it will become nonfunctional until you uninstall the device in Device Manager!*

Wired Devices: RB4InstrumentMapper is capable of installing the WinUSB driver on an Xbox One dongle directly, through the `Configure Devices` button on its main menu. However, if you run into any issues with it, this method of installing the driver will work as an alternative.

1. Download [Zadig](https://zadig.akeo.ie/) and run it.
2. Under Options, select `List All Devices`.
3. Select your device, then change the box to the right of the green/orange arrow to the `WinUSB` driver. Things should look like the example below:

   ![Zadig example](/Docs/Images/Zadig.png "Zadig example")

4. Hit `Replace Driver`, and repeat for any additional peripherals you wish to use.

To undo this process, press Windows + X and select `Device Manager`, then find the device under the `Universal Serial Bus devices` category (*not `Universal Serial Bus controllers`*), right-click it and hit Uninstall, and check the checkbox that says `Uninstall driver software for this device.`.

## Building

To build this program, you will need:

- Visual Studio, or MSBuild/[the .NET SDK](https://dotnet.microsoft.com/en-us/download) + your code editor of choice.
- [WiX Toolset v4](https://wixtoolset.org/) if you wish to build the installer.

## References

Predecessors:

- [GuitarSniffer repository](https://github.com/artman41/guitarsniffer)
- [DrumSniffer repository](https://github.com/Dunkalunk/guitarsniffer)

Packet data:

- [GuitarSniffer guitar packet logs](https://1drv.ms/f/s!AgQGk0OeTMLwhA-uDO9IQHEHqGhv)
- GuitarSniffer guitar packet spreadsheets: [New](https://docs.google.com/spreadsheets/d/1ITZUvRniGpfS_HV_rBpSwlDdGukc3GC1CeOe7SavQBo/edit?usp=sharing), [Old](https://1drv.ms/x/s!AgQGk0OeTMLwg3GBDXFUC3Erj4Wb)
- [rb4.app's Javascript source](https://rb4.app/js/app.js)
- Original research, found in the [PlasticBand documentation repository](https://github.com/TheNathannator/PlasticBand).

## License

This program is licensed under the MIT license. See [LICENSE](LICENSE) for details.
