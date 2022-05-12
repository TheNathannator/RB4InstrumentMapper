# Xbox One RB4 Instrument Data Packets

This document provides some details on how Xbox One device data packets are received through Pcap packet captures.

This documentation is far from fully comprehensive, as there are many parts of the Xbox One controller protocol that don't pertain to sniffing inputs. There are also some parts of the receiver header data that are not well understood.

Byte numbers in the lists are 0-indexed.

## Table of Contents

- [Packet Sections](#packet-sections)
  - [Receiver Header](#receiver-header)
  - [Command Header](#command-header)
    - [Important Command IDs](#important-command-ids)
      - [`0x07`: Virtual Keycode](#0x07-virtual-keycode)
      - [`0x20`: Input Data](#0x20-input-data)
- [Guitar Input Data](#guitar-input-data)
  - [Guitar Packet Samples](#guitar-packet-samples)
- [Drums Input Data](#drums-input-data)
  - [Drum Packet Samples](#drum-packet-samples)
- [References](#references)

## Packet Sections

- Bytes 0-25: Receiver header
- Bytes 26-29: Command header
- Bytes 30 onward: Message data

### Receiver Header

`0x88 <flags-1> 0xA0-00 <receiver ID?> <device ID> <redundant receiver ID?> <packet count> <flags-2> 0x00`

- 26 bytes long
- Not well understood, more research needed

Bytes:

- Byte 0: Always `0x88`?
- Byte 1: Flags value?
  - Typically `0x11` but is `0x19` every so often
- Bytes 2-3: Constant `0xA0-00`?
- Bytes 4-9: Receiver ID?
- Bytes 10-15: Device ID
  - This is the value to keep track of for identifying which device is which.
- Bytes 16-21: Redundant receiver ID? (mirrors 4-9)
- Bytes 22-23: Little-endian packet count
  - This one is encoded weirdly, it seems to be an 8-bit value that's been bit-shifted left by 4 bits such that the value is split between two different bytes.
  - In more explicit terms, the high 4 bits of 22 seem to increment by 1 with every packet, and 23 seems to increment every time 22's high 4 bits roll over from F to 0.
- Byte 24: Another flags value?
  - Seems to be `0x01` during power-on or Xbox button packets, and `0x00` everywhere else
- Byte 25: Constant `0x00`?

### Command Header

`<command ID> <flags/client ID> <sequence count> <data length>`

- 4 bytes long

Bytes:

- Byte 0: Command ID
  - This indicates what the data following this header represents.
  - Important IDs:
  - `0x07`: Virtual keycode (used for the guide button)
  - `0x20`: Input report
- Byte 1: Flags/client ID
  - This is a combination flags and client ID value for messages.
  - Understanding this value isn't important for just sniffing input data.
- Byte 2: Sequence count
  - This keeps track of the order/sequence of packets.
  - Understanding this value isn't important for just sniffing input data.
- Byte 3: Data length
  - Number of bytes in the rest of the packet (some exceptions to this, but none that pertain to input data).

The bytes following the header are defined per command ID.

#### Important Command IDs

##### `0x07`: Virtual Keycode

This command is used to report virtual keycode presses. It is also used to report the guide button press.

`<pressed> <keycode>`

- `pressed` is a boolean value (`0x00` or `0x01`) indicating whether or not the key is pressed.
- `keycode` is the virtual keycode for the key.
  - This is `0x5B` when the guide button is pressed, which equates to the Left Windows key.

##### `0x20`: Input Data

This command is used to report input data. The actual input data varies per device type.

The standard Xbox One controller layout is as follows:

- 12 bytes long
- `<buttons> <left trigger> <right trigger> <left stick X> <left stick Y> <right stick X> <right stick Y>`
  - `buttons`: 16-bit button bitmask. Note that while other values are little-endian, these are listed in big-endian format.
    - Bit 0 (`0x0001`) - D-pad Up
    - Bit 1 (`0x0002`) - D-pad Down
    - Bit 2 (`0x0004`) - D-pad Left
    - Bit 3 (`0x0008`) - D-pad Right
    - Bit 4 (`0x0010`) - Left Bumper
    - Bit 5 (`0x0020`) - Right Bumper
    - Bit 6 (`0x0040`) - Left Stick Press
    - Bit 7 (`0x0080`) - Right Stick Press
    - Bit 8 (`0x0100`) - Sync button
    - Bit 9 (`0x0200`) - Unused (undefined?)
    - Bit 10 (`0x0400`) - Menu Button
    - Bit 11 (`0x0800`) - Options Button
    - Bit 12 (`0x1000`) - A Button
    - Bit 13 (`0x2000`) - B Button
    - Bit 14 (`0x4000`) - X Button
    - Bit 15 (`0x8000`) - Y Button
  - `left trigger` and `right trigger`: 16-bit little-endian unsigned axis
  - `left stick X`/`Y`, `right stick X`/`Y`: 16-bit little-endian signed axis

## Guitar Input Data

10 bytes long

`<buttons> <tilt> <whammy> <pickup/slider> <upper frets> <lower frets> <unused[3]>`

- `buttons`: 16-bit button bitmask
  - Bit 0 (`0x0001`) - D-pad Up/Strum Up
  - Bit 1 (`0x0002`) - D-pad Down/Strum Down
  - Bit 2 (`0x0004`) - D-pad Left
  - Bit 3 (`0x0008`) - D-pad Right
  - Bit 4 (`0x0010`) - Orange Fret Flag (equivalent to Left Bumper)
  - Bit 5 (`0x0020`) - Unused (equivalent to Right Bumper)
  - Bit 6 (`0x0040`) - Lower Fret Flag (equivalent to Left Stick Press)
  - Bit 7 (`0x0080`) - Unused (equivalent to Right Stick Press)
  - Bit 8 (`0x0100`) - Sync button?
  - Bit 9 (`0x0200`) - Unused (undefined?)
  - Bit 10 (`0x0400`) - Menu Button
  - Bit 11 (`0x0800`) - Options Button
  - Bit 12 (`0x1000`) - Green Fret Flag (equivalent to A Button)
  - Bit 13 (`0x2000`) - Red Fret Flag (equivalent to B Button)
  - Bit 14 (`0x4000`) - Blue Fret Flag (equivalent to X Button)
  - Bit 15 (`0x8000`) - Yellow Fret Flag (equivalent to Y Button)
- `tilt`: 8-bit tilt axis
  - Has a threshold of `0x70`? (values below get cut off to `0x00`)
- `whammy`: 8-bit whammy bar axis
- `pickup/slider`: 8-bit pickup switch/slider axis
  - Seems to use top 4 bytes, values from the Guitar Sniffer logs are `0x00`, `0x10`, `0x20`, `0x30`, and `0x40`
- `upper frets`, `lower frets`: 8-bit fret bitmask
  - Bit 0 (`0x01`) - Green
  - Bit 1 (`0x02`) - Red
  - Bit 2 (`0x04`) - Yellow
  - Bit 3 (`0x08`) - Blue
  - Bit 4 (`0x10`) - Orange
  - Bits 5-7 - Unused
- `unused[3]`: unknown data values

### Guitar Packet Samples

```
2021-10-31 01:35:10.730 [40] 8811A0006245B4E9D18A 7EED8FFE198A 6245B4E9D18A20550000 2000530A 00003C00000000000000
2021-10-31 01:35:10.742 [40] 8811A0006245B4E9D18A 7EED8FFE198A 6245B4E9D18A30550000 2000540A 00003E00000000000000
2021-10-31 01:35:10.750 [40] 8811A0006245B4E9D18A 7EED8FFE198A 6245B4E9D18A40550000 2000550A 00003D00000000000000
2021-10-31 01:35:10.758 [40] 8811A0006245B4E9D18A 7EED8FFE198A 6245B4E9D18A50550000 2000560A 00003C00000000000000
2021-10-31 01:35:10.766 [40] 8811A0006245B4E9D18A 7EED8FFE198A 6245B4E9D18A60550000 2000570A 00003B00000000000000
2021-10-31 01:35:10.783 [40] 8811A0006245B4E9D18A 7EED8FFE198A 6245B4E9D18A70550000 2000580A 00003C00000000000000
2021-10-31 01:35:10.799 [40] 8811A0006245B4E9D18A 7EED8FFE198A 6245B4E9D18A80550000 2000590A 00003B00000000000000
2021-10-31 01:35:10.807 [40] 8811A0006245B4E9D18A 7EED8FFE198A 6245B4E9D18A90550000 20005A0A 00003C00000000000000
```

```
2021-10-31 01:58:23.354 [40] 8811A0006245B4E9D18A 7EED8FFB14BF 6245B4E9D18A209B0000 2000C60A 10008A00400100000000
2021-10-31 01:58:23.363 [40] 8811A0006245B4E9D18A 7EED8FFB14BF 6245B4E9D18A309B0000 2000C70A 10008900400100000000
2021-10-31 01:58:23.371 [40] 8811A0006245B4E9D18A 7EED8FFB14BF 6245B4E9D18A409B0000 2000C80A 10008A00400100000000
2021-10-31 01:58:23.403 [40] 8811A0006245B4E9D18A 7EED8FFB14BF 6245B4E9D18A509B0000 2000C90A 10008B00400100000000
2021-10-31 01:58:23.411 [40] 8811A0006245B4E9D18A 7EED8FFB14BF 6245B4E9D18A609B0000 2000CA0A 10008D00400100000000
2021-10-31 01:58:23.443 [40] 8811A0006245B4E9D18A 7EED8FFB14BF 6245B4E9D18A709B0000 2000CB0A 10008F00400100000000
2021-10-31 01:58:23.459 [40] 8811A0006245B4E9D18A 7EED8FFB14BF 6245B4E9D18A809B0000 2000CC0A 10009000400100000000
```

## Drums Input Data

Some of the data here is speculatory. It needs to be verified using packet captures.

6 bytes long

`<buttons> <pad velocities> <cymbal velocities>`

Bytes:

- `buttons`: 16-bit button bitmask
  - Bit 0 (`0x0001`) - D-pad Up
  - Bit 1 (`0x0002`) - D-pad Down
  - Bit 2 (`0x0004`) - D-pad Left
  - Bit 3 (`0x0008`) - D-pad Right
  - Bit 4 (`0x0010`) - 1st Kick Pedal (equivalent to Left Bumper)
  - Bit 5 (`0x0020`) - 2nd Kick Pedal (equivalent to Right Bumper)
  - Bit 6 (`0x0040`) - Unused? (equivalent to Left Stick Press)
  - Bit 7 (`0x0080`) - Unused? (equivalent to Right Stick Press)
  - Bit 8 (`0x0100`) - Sync button?
  - Bit 9 (`0x0200`) - Unused (undefined?)
  - Bit 10 (`0x0400`) - Menu Button
  - Bit 11 (`0x0800`) - Options Button
  - Bit 12 (`0x1000`) - Green Pad (equivalent to A Button)
  - Bit 13 (`0x2000`) - Red Pad (equivalent to B Button)
  - Bit 14 (`0x4000`) - Blue Pad (equivalent to X Button)
  - Bit 15 (`0x8000`) - Yellow Pad (equivalent to Y Button)
- Bytes 32-33 - Pad velocities
  - Bits 0-3 (`0x000F`) - Green Pad
  - Bits 4-7 (`0x00F0`) - Blue Pad
  - Bits 8-11 (`0x0F00`) - Yellow Pad
  - Bits 12-15 (`0xF000`) - Red Pad
  - Seem to range from 0-7
- Bytes 34-35 - Cymbal velocities
  - Bits 0-3 (`0x000F`) - Unused?
  - Bits 4-7 (`0x00F0`) - Green Cymbal
  - Bits 8-11 (`0x0F00`) - Blue Cymbal
  - Bits 12-15 (`0xF000`) - Yellow Cymbal
  - Seem to range from 0-7

### Drum Packet Samples

```
2021-10-31 02:25:31.725 [36] 8811A0006245B4E9D18A 7EED8FFFCF6B 6245B4E9D18AA0020000 20002B06 0000 0400 0000
2021-10-31 02:25:31.773 [36] 8811A0006245B4E9D18A 7EED8FFFCF6B 6245B4E9D18AB0020000 20002C06 0000 0000 0000
2021-10-31 02:25:32.038 [36] 8811A0006245B4E9D18A 7EED8FFFCF6B 6245B4E9D18AC0020000 20002D06 2000 4000 0000
2021-10-31 02:25:32.086 [36] 8811A0006245B4E9D18A 7EED8FFFCF6B 6245B4E9D18AD0020000 20002E06 0000 0000 0000
2021-10-31 02:25:32.327 [36] 8811A0006245B4E9D18A 7EED8FFFCF6B 6245B4E9D18AE0020000 20002F06 0000 0400 0000
2021-10-31 02:25:32.367 [36] 8811A0006245B4E9D18A 7EED8FFFCF6B 6245B4E9D18AF0020000 20003006 0000 0000 0000
2021-10-31 02:25:32.608 [36] 8811A0006245B4E9D18A 7EED8FFFCF6B 6245B4E9D18A00030000 20003106 0000 0040 0000
2021-10-31 02:25:32.656 [36] 8811A0006245B4E9D18A 7EED8FFFCF6B 6245B4E9D18A10030000 20003206 0000 0000 0000
```

## References

- [GuitarSniffer guitar packet logs](https://1drv.ms/f/s!AgQGk0OeTMLwhA-uDO9IQHEHqGhv)
- GuitarSniffer guitar packet spreadsheets: [New](https://docs.google.com/spreadsheets/d/1ITZUvRniGpfS_HV_rBpSwlDdGukc3GC1CeOe7SavQBo/edit?usp=sharing), [Old](https://1drv.ms/x/s!AgQGk0OeTMLwg3GBDXFUC3Erj4Wb)
- https://github.com/quantus/xbox-one-controller-protocol
- https://github.com/medusalix/xone
