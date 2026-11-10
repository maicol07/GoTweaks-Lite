# Licensing

GoTweaks has a layered licensing situation. Please read this before
redistributing source or binaries.

## GoTweaks' own source code — MIT

The original GoTweaks source code is licensed under the **MIT License**
(see [`LICENSE`](LICENSE)). Individual source files authored for GoTweaks
remain available under MIT terms.

## Binaries that include VIIPER

Builds of this fork that include `libviiper.dll` are conveyed under GPL-3.0
as a combined work. See [COPYING](COPYING). The pinned
[VIIPER corresponding source](https://github.com/corando98/VIIPER/tree/7805d80b32cf6a27319276599e7fd76dd96d2a6c)
and the [native build instructions](docs/feature-baseline.md#viiper-and-usbip)
identify the source and tooling for this fork's DLL.

Releases up to and including **1.6** shipped `libviiper.dll`, built from a fork of
**VIIPER** (by Alia5) and licensed under the **GNU General Public License,
version 3.0**, for the (now removed) Controller Emulation feature. Linking a GPL-3.0
library produces a *combined work*, so those binary distributions were conveyed under
the GPL-3.0 (see [`COPYING`](COPYING)).

Upstream Lite releases 1.7 through 1.9.2 did not ship or link
`libviiper.dll`. The older releases stay GPL-3.0 and their source offer
still stands:

- GoTweaks source: this repository (the git history contains the code those releases
  were built from).
- VIIPER fork that produced `libviiper.dll`: `https://github.com/corando98/VIIPER`
  (GPL-3.0; see its `LICENSE.txt` and `NOTICE.md`).

Other third-party components that are still bundled are covered, under their own
licenses, in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
