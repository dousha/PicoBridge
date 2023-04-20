# PicoBridge - C# Library for parsing face-tracking data from PICO headsets

A C# library for parsing FT data from PICO 4 Pro.

## PicoVRCFTModule - PICO 4 Pro Module for VRCFaceTracking 4.0

### Setup

1. (TBA by PICO) Download and install the latest Streaming Assistant for PICO
2. Remove or rename `pico_et_ft_bt_bridge.exe` in 
`%StreamingAssistantInstallationDirectory%\driver\bin\win64`. Typically it's 
in `?:\Program Files (x86)\Streaming Assistant\driver\bin\win64` where `?` is a drive letter
3. Download the latest module file from [Release](https://github.com/dousha/PicoBridge/releases)
4. Put both `PicoBridge.dll` and `PicoVRCFTModule.dll` into `%appdata%\VRCFaceTracking\CustomLibs`
5. Start VRCFaceTracking!

### Road Map

- [x] Basic gaze tracking
- [x] Eyelid support
- [ ] Basic expression support
- [ ] Viseme support

### Known Issues

#### Eye closing is not properly mapped

Some avatars may not respond to eye closing. This is a known issue that we are investigating.

#### Expressions are always mapped to frowning / sad face

Expression support is still under development. It seems that PICO's data doesn't map cleanly to the formats of other vendors. We will need to convert them.

### Troubleshooting

#### Port already in use. Run PICOBridgeHelper.exe first!

Please make sure that you have removed `pico_et_ft_bt_bridge.exe` and restarted Streaming Assistant. Should the problem persists, please report it in the [Issues](https://github.com/dousha/PicoBridge/issues).

## Credits

* [Ben Thomas](https://github.com/benaclejames/) for VRCFaceTracking
* [C095](https://github.com/Chinglem) for providing crucial information and testing
