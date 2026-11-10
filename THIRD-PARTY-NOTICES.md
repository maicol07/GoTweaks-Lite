# Third-Party Notices

GoTweaks source is available under the MIT License (see `LICENSE`).
See [LICENSING.md](LICENSING.md) for the terms of combined binary distributions.

---

## VIIPER (`libviiper.dll`)

The restored implementation uses [corando98/VIIPER at `7805d80`](https://github.com/corando98/VIIPER/tree/7805d80b32cf6a27319276599e7fd76dd96d2a6c).
See the [GPL-3.0 license](licenses/VIIPER-GPLv3.txt) and
[native build instructions](docs/feature-baseline.md#viiper-and-usbip).

Releases up to and including 1.6 shipped `libviiper.dll`, built from a **fork of
VIIPER** (originally by **Alia5**, https://github.com/Alia5/VIIPER) and licensed under
the **GNU General Public License, version 3.0**, for the Controller Emulation feature.
Upstream Lite releases 1.7 through 1.9.2 did not ship or link the DLL.

- Full license text (kept for the older releases): [`licenses/VIIPER-GPLv3.txt`](licenses/VIIPER-GPLv3.txt)
- Copyright © the VIIPER authors (Alia5 and contributors)

### Written offer of source (GPL-3.0 §6) — releases 1.6 and earlier

The complete corresponding source for the version of `libviiper.dll` shipped with those
releases — including the fork's modifications and the build scripts used to produce
it — is publicly available at:

  https://github.com/corando98/VIIPER

## GamepadMotion (`GamepadMotion.dll`)

Copyright © 2020–2023 Julian "Jibb" Smart. See the
[MIT license and notice](Native/GamepadMotion/LICENSE) and
[source used by this fork](Native/GamepadMotion).

## Nefarius managed libraries

`Nefarius.Drivers.HidHide` 1.8.59 and
`Nefarius.Utilities.DeviceManagement` 4.0.0 are MIT-licensed managed
dependencies used for optional device suppression. The kernel driver is
installed separately. See the license notices for
[HidHide](https://github.com/nefarius/Nefarius.Drivers.HidHide) and
[DeviceManagement](https://github.com/nefarius/Nefarius.Utilities.DeviceManagement).
