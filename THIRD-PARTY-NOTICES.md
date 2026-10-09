# Third-party notices

TSCC_WASD original source is MIT licensed. Dependencies and adapted source retain their own licenses.

## SDL Switch 2 protocol reference and adaptation

The NS2 Pro USB command sequence and HD-rumble encoding are adapted from SDL's
`SDL_hidapi_switch2.c`; its input, IMU and calibration formats are used by the C# parser. The adapted portions
retain SDL's zlib license and are marked as altered. See [full notice](licenses/SDL.txt).
No SDL or libusb binary is bundled; the transport uses Windows WinUSB and HID APIs.

## Nefarius.ViGEm.Client 1.21.256 (distributed with the application)

Source: https://github.com/nefarius/ViGEm.NET

NuGet identifies its license as MIT and lists Copyright © Nefarius Software
Solutions e.U. 2017–2023. The upstream license text follows:

MIT License

Copyright (c) 2018 Benjamin Höglinger-Stelzer

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## External prerequisites (not bundled)

- Windows XInput API is supplied by Windows.
- .NET 10 Windows Desktop Runtime is installed separately:
  https://github.com/dotnet/runtime and https://github.com/dotnet/wpf.
- ViGEmBus driver is separately distributed under BSD-3-Clause:
  https://github.com/nefarius/ViGEmBus/blob/master/LICENSE.
- HidHide (MIT, https://github.com/nefarius/HidHide) is optional, installed separately
  and not redistributed. While mapping, TSCC_WASD runs the user's installed `HidHideCLI.exe`
  as a separate process to change and later restore its configuration; no HidHide code
  or binary is included in TSCC_WASD.
- Steam is not bundled or linked. TSCC_WASD only starts the user's installed `steam.exe`
  (with `-shutdown`, then normally) when the user asks it to restart Steam.

## Development-only packages

Microsoft.NET.Test.Sdk, xUnit and its Visual Studio runner are test dependencies;
they are not included in the application ZIP. Refer to their NuGet metadata and
upstream repositories for notices. Each project's packages.lock.json records
versions and integrity hashes.

PlayStation, DualShock, Xbox, Nintendo Switch, Steam and reWASD names belong to their respective owners.
TSCC_WASD is an independent project and does not imply endorsement.
