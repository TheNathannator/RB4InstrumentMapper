# RB4InstrumentMapper
[![en](https://img.shields.io/badge/lang-en-red.svg)](https://github.com/TheNathannator/RB4InstrumentMapper/blob/main/README.md)
[![es](https://img.shields.io/badge/lang-es-yellow.svg)](https://github.com/TheNathannator/RB4InstrumentMapper/blob/main/README.es.md)

A program that maps packets from Xbox One instrument peripherals to virtual controllers, for use in games such as [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper main menu](Docs/Images/Readme/program-screenshot.png "RB4InstrumentMapper main menu")

All Xbox One instruments are supported (RB4 guitars/drums, GHL guitar), along with the RB4 wireless legacy adapter.

## Table of Contents

- [Installation](#installation)
  - [ViGEmBus Setup](#vigembus-setup)
  - [vJoy Setup](#vjoy-setup)
- [Using Rock Band 4 Guitars and Drums](#using-rock-band-4-guitars-and-drums)
  - [Setup](#setup)
    - [Stratocaster, Jaguar, Drums](#stratocaster-jaguar-drums)
    - [Riffmaster](#riffmaster)
  - [Usage](#usage)
    - [Having Sync Issues?](#having-sync-issues)
- [Using Guitar Hero Live Guitars and RB4 Wireless Legacy Adapters](#using-guitar-hero-live-guitars-and-rb4-wireless-legacy-adapters)
  - [Setup](#setup-1)
  - [Usage](#usage-1)
- [Mapping your Controls](#mapping-your-controls)
  - [Clone Hero](#clone-hero)
  - [YARG](#yarg)
  - [GHWT: Definitive Edition](#ghwt-definitive-edition)
  - [RPCS3](#rpcs3)
- [Packet Logs](#packet-logs)
- [Error Logs](#error-logs)
- [Command-Line Interface](#command-line-interface)
  - [Building the CLI Version](#building-the-cli-version)
  - [CLI Options](#cli-options)
  - [Example Batch Script](#example-batch-script)
- [Building](#building)
- [References](#references)
- [License](#license)

## Installation

1. Download and install RB4InstrumentMapper from [the Releases page](https://github.com/TheNathannator/RB4InstrumentMapper/releases).
2. Download and set up [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest) or [vJoy](https://github.com/jshafer817/vJoy/releases/latest) using the guides below.
   - ViGEmBus is recommended, as it is significantly easier to use and requires no configuration. vJoy is supported as an alternative on the off-chance you run into issues with ViGEmBus.
   - Both can be installed simultaneously if desired, however RB4InstrumentMapper will only use one of them at a time.

### ViGEmBus Setup

1. Download and install [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest).
2. That's all.

### vJoy Setup

1. Download and install [vJoy](https://github.com/jshafer817/vJoy/releases/latest).
2. After installing, open your Start menu, find the `vJoy` folder, and open the `Configure vJoy` program inside it.
3. Configure one device for each one of your controllers, using these settings:
   - Number of Buttons: 16
   - POV Hat Switch: Continuous, POVs: 1
   - Axes: `X`, `Y`, `Z`

   ![vJoy configuration screenshot](Docs/Images/Readme/vjoy-configuration.png "vJoy configuration screenshot")

4. Click Apply.
5. You're all set up!

---

## Using Rock Band 4 Guitars and Drums

![Rock Band 4 Stratocaster](Docs/Images/Peripherals/rock-band-4-stratocaster.png "Rock Band 4 Stratocaster")
![Rock Band 4 Jaguar](Docs/Images/Peripherals/rock-band-4-jaguar.png "Rock Band 4 Jaguar")
![Rock Band 4 drumkit](Docs/Images/Peripherals/rock-band-4-drums.png "Rock Band 4 drumkit")
![Riffmaster guitar](Docs/Images/Peripherals/riffmaster.png "Riffmaster guitar")

*Note that RB4InstrumentMapper is NOT required to use guitars in Fortnite!*

### Setup

#### Stratocaster, Jaguar, Drums

![Rock Band 4 Stratocaster](Docs/Images/Peripherals/rock-band-4-stratocaster.png "Rock Band 4 Stratocaster")
![Rock Band 4 Jaguar](Docs/Images/Peripherals/rock-band-4-jaguar.png "Rock Band 4 Jaguar")
![Rock Band 4 drumkit](Docs/Images/Peripherals/rock-band-4-drums.png "Rock Band 4 drumkit")

For wireless Rock Band 4 guitars and drums, you will need an Xbox One wireless receiver. Both versions of the official one are shown here:

![Xbox One receiver, original](Docs/Images/Peripherals/xbox-one-receiver-gen-1.png "Xbox One receiver, original")
![Xbox One receiver, revision](Docs/Images/Peripherals/xbox-one-receiver-gen-2.png "Xbox One receiver, revision")

- This is *not* the same as an Xbox 360 wireless receiver! You must get an Xbox One (or just "Xbox") receiver, as shown above.
- Third-party receivers are untested, and they will not be deliberately supported.

Additionally, Jaguar guitars ![Rock Band 4 Jaguar](Docs/Images/Peripherals/rock-band-4-jaguar-small.png "Rock Band 4 Jaguar") require a firmware update in order to connect to Xbox One receivers.

- [Instructions](https://bit.ly/2UHzonU)
- [Clone Hero wiki re-host](https://wiki.clonehero.net/link/61), in case the above link goes down again.

#### Riffmaster

![Riffmaster guitar](Docs/Images/Peripherals/riffmaster.png "Riffmaster guitar")

For the Riffmaster, you will need its dedicated wireless dongle, pictured below:

![Riffmaster dongle](Docs/Images/Peripherals/riffmaster-dongle-front.png "Riffmaster dongle")

### Usage

1. In the `Controller Emulation Mode` dropdown, select the controller emulation mode you want to use.

   ![Controller emulation mode selection](Docs/Images/Readme/controller-emulation-mode.png)

2. Hit the `Start` button to begin reading inputs.
3. Connect your instruments. They will be picked up and read automatically until you hit `Stop` or close the program.
   - Instruments can be connected before or after hitting Start, the ordering doesn't matter.
4. [Map your controls in the game you'll be playing](#mapping-your-controls).

#### Having Sync Issues?

Some guitars/drumkits might not sync properly when using just the sync button. This includes the PDP drumkit and occasionally the Jaguar guitar. Follow these steps to sync your device correctly:

1. Go to Windows settings > Devices > Bluetooth & other devices
2. Click `Add Bluetooth or other device` and pick the `Everything else` option.
3. Press and hold the sync button until the Xbox button light flashes quickly.
4. Select `Xbox compatible game controller` from the list once it appears.
5. If that doesn't work, restart your PC and try again.

---

## Using Guitar Hero Live Guitars and RB4 Wireless Legacy Adapters

![Guitar Hero Live guitar](Docs/Images/Peripherals/guitar-hero-live-guitar.png "Guitar Hero Live guitar")
![Rock Band 4 wireless legacy adapter](Docs/Images/Peripherals/rock-band-4-wireless-legacy.png "Rock Band 4 wireless legacy adapter")

### Setup

You will need to install the WinUSB driver onto the Guitar Hero Live dongle or Rock Band 4 wireless legacy adapter before using it. RB4InstrumentMapper is capable of doing this directly, through the `Configure Devices` button on its main menu:

1. Click the `Configure Devices` button under the `USB` group.

   ![USB enable checkbox](Docs/Images/Readme/usb-configure-button.png)

2. Find the device you want to use on the left side of the menu.

   ![WinUSB configuration, left side](Docs/Images/Readme/usb-configure-left.png)

3. Click the `Switch Driver` button and wait for it to switch the driver. The device will show up on the right side of the menu once it's done.

   ![WinUSB configuration, right side](Docs/Images/Readme/usb-configure-right.png)

   - Note that games that natively support the device will no longer work directly with it until you uninstall the WinUSB driver. Hit the `Revert Driver` button on the device to do so, after which those games will work with it again.

If you run into any issues with this process, you may try [installing the driver manually](Docs/WinUSB/manual-winusb-install.md). This is not recommended for normal use, it should only be used if the Configure Devices menu is not working.
- This also covers [uninstalling manually](Docs/WinUSB/manual-winusb-install.md#remove-winusb), in case a device gets stuck with the driver installed and RB4InstrumentMapper stops picking it up.

### Usage

1. **Ensure you have [installed WinUSB](#setup-1) on the devices you want to use! They will not be recognized otherwise!**
2. In the `Controller Emulation Mode` dropdown, select the controller emulation mode you want to use.

   ![Controller emulation mode selection](Docs/Images/Readme/controller-emulation-mode.png)

3. Hit the `Start` button to begin reading inputs.
4. [Map your controls in the game you'll be playing](#mapping-your-controls).

Devices will be detected automatically as they are connected/removed, though they will not be read until you hit Start.

If you are having issues with inputs not being registered or other general weirdness, try unplugging and replugging the dongle/cable. Unfortunately this backend is still finnicky, so things may not work the smoothest.

---

## Mapping your Controls

Now that RB4InstrumentMapper is set up and running, map your controls for each instrument in the game you'll be playing.

### Clone Hero

If you're using a guitar in ViGEmBus mode, Clone Hero will automatically map your controls. You shouldn't need to do any manual mapping, unless you're using a Guitar Hero Live guitar, in which case you may need to swap your whammy and tilt bindings.

- This automatic mapping will also apply to drumkits, however here the bindings will not be correct, as they are meant for guitars. You will need to remove the default bindings and do them manually.

For drumkits, vJoy mode, or if you otherwise need to customize your controls:

1. Press Space on the main menu.

   !["Press Space For Controls" label](Docs/Images/Readme/clone-hero-press-space.png)

2. Click Assign Controller and press a button on the device for it to be assigned.

   !["Assign Controller" button](Docs/Images/Readme/clone-hero-assign-controller.png)

3. Click the slots in the `Controller` column to map each of the controls.

   ![Controls bound under the "Controller" column](Docs/Images/Readme/clone-hero-bind-controls.png)

4. Repeat for each connected device.
5. Click `Done`.

If you require further help with mapping, you can ask in the [Clone Hero Discord server](https://discord.gg/Hsn4Cgu).

### YARG

Refer to the [official documentation](https://docs.yarg.in/en/profiles).

### GHWT: Definitive Edition

Refer to the [official documentation](https://ghwt.de/wiki/#/wtde/binding?id=binding-controllers).

### RPCS3

1. For RPCS3, use the `ViGEmBus (RPCS3 compatibility)` controller mode. This will change the mappings that RB4InstrumentMapper outputs so that little to no remapping is required to use instruments in RPCS3.

   ![Controller emulation mode selection](Docs/Images/Readme/controller-emulation-mode.png)

2. Open the Gamepad Configuration Menu in RPCS3

   ![RPCS3 Pads icon](Docs/Images/Readme/rpcs3-pads-icon.png)

3. Change the `Handlers` to "XInput", pick your instrument under `Devices`, and set `Device Class` to "Rock Band - Guitar" if you're using a guitar or 'Rock Band - Drums' if you're using drums.

   ![RPCS3 Gamepad settings](Docs/Images/Readme/rpcs3-gamepad-settings.png)

4. Repeat for each connected device in the other Player tabs.
5. Click `Done`.

## Packet Logs

RB4InstrumentMapper is capable of logging packets to a file for debugging purposes. To do so, enable both the `Show Packets (for debugging)` and `Log packets to file` checkboxes, then hit Start. Packet logs get saved to a `RB4InstrumentMapper` > `PacketLogs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for packet parsing issues.

Note that these settings are meant for debugging purposes only, leaving them enabled can reduce the performance of the program somewhat.

## Error Logs

In the case that the program crashes, an error log is saved to a `RB4InstrumentMapper` > `Logs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for the crash.

## Command-Line Interface

RB4InstrumentMapper now includes a command-line interface (CLI) mode that allows you to use the mapping functionality without the GUI. This is particularly useful for automating the setup of instruments in arcade cabinets or similar scenarios where you want to run the tool without user interaction.

### Building the CLI Version

To build the CLI version, use the following command in the solution directory:

```
dotnet build Program/RB4InstrumentMapper.csproj -c CLI
```

This will create the CLI executable in the `Program/bin/x64/CLI/net472` directory.

### CLI Options

The CLI version supports the following command-line options:

- `--mode <mode>`: (Required) Specify the mapping mode. Valid values are:
  - `vigem`: Use ViGEmBus emulation (recommended)
  - `vjoy`: Use vJoy emulation
  - `rpcs3`: Use RPCS3-specific mappings
- `--timeout <seconds>`: Run for the specified number of seconds and then automatically exit
- `--log-file <path>`: Path to write the log file (default: auto-generated in the application directory)
- `--verbose`: Enable verbose logging (helpful for troubleshooting)
- `--accurate-drums`: Use accurate drum mappings
- `--wait-for-devices [timeout]`: Wait for devices to be detected before starting mapping (default timeout: 30 seconds)
- `--help`: Display the help message

### Example Batch Script

The repository includes a sample batch script `run_mapper_cli.bat` that demonstrates how to use the CLI version:

```batch
@echo off
REM RB4InstrumentMapper CLI Launcher for Clone Hero Arcade Cabinet
REM ===============================================================

REM Change to the directory where this script is located
cd /d "%~dp0"

echo Starting RB4InstrumentMapper in CLI mode...
echo.

REM Configuration Parameters
set MODE=vigem
set WAIT_FOR_DEVICES=true
set WAIT_TIMEOUT=30
set LOG_FILE="%~dp0RB4InstrumentMapper_log.txt"

REM Run the CLI version with the specified parameters
echo Running with: --mode %MODE% --wait-for-devices %WAIT_TIMEOUT% --log-file %LOG_FILE%
echo.
echo Press Ctrl+C to stop the mapping process and exit
echo.

REM Launch the CLI application
Program\bin\x64\CLI\net472\RB4InstrumentMapperCLI.exe --mode %MODE% --wait-for-devices %WAIT_TIMEOUT% --log-file %LOG_FILE% --accurate-drums --verbose

REM If we get here, check if the application exited with an error
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo RB4InstrumentMapper exited with error code: %ERRORLEVEL%
    echo Check the log file for details.
    echo.
    pause
    exit /b %ERRORLEVEL%
)

exit /b 0
```

You can customize this script to suit your specific needs. For example, in an arcade cabinet setup, you might want to include this script in your startup sequence to automatically map instruments without user interaction.

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
