# Windows 10 MIDI SysEx Utility

Very simple MIDI SysEx transfer utility using the Windows 10 MIDI API. Supports sending SysEx files, including operating system upgrades for MIDI devices, with a configurable message/buffer size and delay.

Uses my Windows 10 MIDI library, which supports all Windows 10 MIDI features, including Bluetooth LE MIDI.

Screen shots of the app transferring an operating system upgrade for the Elektron Digitone over BLE MIDI. 

![Screen shot](/images/sysex1.png)

![Screen shot](/images/sysex2.png)

![Dark Mode Screen shot](/images/sysex3.png)

Prefer to run a pre-built version of this app? It's in the store here:
https://www.microsoft.com/store/productId/9PFD4DDWGKTN

# MIDI Port Names
Don't like the Windows 10 / WinRT MIDI Port names? Check out this (unofficial, unsupported) PowerShell script that lets you rename them to anything you want:
https://gist.github.com/Psychlist1972/ec5c52c9e4999710191d4bb07e82f98a
