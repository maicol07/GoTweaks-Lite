# Feature restoration notes

References fetched for the initial comparison are Rulob Lite `94adfd9`,
Rayekkk Lite `64ed109`, corando98 GoTweaks `99ed5f5` and corando98 VIIPER
`904bef3`. Use the removal commits' parents to recover code that already
worked with Lite.

| Feature | History and what remains | Restoration boundary |
| --- | --- | --- |
| Controller emulation | `d2e4a93` removed VIIPER, native gyro, Sticks & Triggers shaping, HidHide and the prerequisite UI. It added physical X-Input/D-Input switching. `dbceccb` records Rayekkk's VIIPER/usbip fixes already brought into Lite. | Recover the manager, forwarding, IPC and shaping tests from `d2e4a93^`. Keep the physical mode switch. Verify the historical native binaries, usbip-win2 and optional HidHide separately before enabling real device tests. |
| Lossless Scaling | `b13b4da` removed the Scale tab. `68039bb` removed Settings.xml sync and per-game integration from the helper. The current manager keeps detection, launch and hotkey handling. `1efa505` and `16227bf` fix historical settings writes and apply/restart tracking. | Keep the current quick toggle. Compare the historical integration against the installed version and verify configuration ownership, backup and write behavior before restoring controls. |
| Per-game controller settings | `b13b4da` removed the Legion per-game controller toggle and Remap tile. `68039bb` removed remaining property wiring. `Shared/Data/GameProfile.cs` still has nullable Legion remapping and controller fields. The widget also has named controller profile storage. | Reuse the helper's existing game selection and profile lifecycle. Verify capture, application and restoration separately, including older XML and manual changes. |

The tree retains `licenses/VIIPER-GPLv3.txt`. The pre-removal tree also
has `Native/GamepadMotion/LICENSE`. Check the exact native sources, licenses
and corresponding source for the binaries selected for redistribution before
adding them back to the package. Driver versions and signatures still need
verification.

### VIIPER and USB/IP

The historical Lite DLL from `1e8a040` was built from
[VIIPER `7805d80`](https://github.com/corando98/VIIPER/commit/7805d80b32cf6a27319276599e7fd76dd96d2a6c)
on `viiper-controller`. Its x64 export table contains all 13 entry points
used by the historical C# binding and exported by `clib/main.go`.
The DLL also builds from that source with Go 1.26.2 and w64devkit 2.11.0
using `go build -trimpath -buildmode=c-shared -o libviiper.dll ./clib`.
The rebuilt DLL passed two local loopback startup, device-type enumeration,
bus create/remove and shutdown cycles without a driver. Enumeration
included Xbox 360, DualShock 4, DualSense, Switch Pro and Steam Controller.
Actual Windows device attachment, reports and gyro still need a device test.

The removed installer pinned
[usbip-win2 0.9.7.8](https://github.com/vadimgrn/usbip-win2/releases/tag/v.0.9.7.8).
The maintainer warns that this release can cause memory corruption and a
BSOD. Exclude it from restoration. Evaluate
[usbip-win2 0.9.8.1](https://github.com/vadimgrn/usbip-win2/releases/tag/v.0.9.8.1)
against the pinned VIIPER revision. Its x64 installer's Authenticode signature
validated on the development PC. Verify the driver catalog, certificate-store
changes and attach/detach behavior separately. Keep the emulator on
loopback as in the historical implementation. Verify a non-controller input
method and rollback before enabling optional physical-device suppression.

## Legion Space service check

On a Legion with `DAService` running in standard automatic mode, open the
widget's Legion tab and choose **Block auto-start**. Check that Windows reports
`Manual` and `Stopped`. Choose **Restore auto-start** and check for
`Automatic` and `Running`. Confirm the OEM widget and controller navigation
still work. After blocking, launching Legion Space may start the service on
demand, so check the startup mode as well as the current process state.
Repeat from `Automatic / Stopped` without starting the service first. When
Windows reports a pending start or stop, verify that the widget waits and
updates without another click. If the transition outlasts the wait, verify
that Refresh becomes available and Windows retains the requested startup mode.

```powershell
Get-Service DAService | Select-Object Name, StartType, Status
```
