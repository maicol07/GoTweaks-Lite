# Widget UI redesign (touch + gamepad)

Target: Legion Go 2 inside Xbox Game Bar. Mockups:
https://claude.ai/artifact/3dj9prEoPe1GUJkUEcDcn5

## Decisions

- Stay on UWP + WinUI 2.8. WinUI 3 cannot host a Game Bar widget.
- Add CommunityToolkit 8 UWP: `SettingsCard`, `SettingsExpander`, `Segmented`.
  Release x64 .NET Native builds; MSB3277 on System.Security.* 4.1.1 vs 5.0 is
  expected (toolkit references 5.0, UWP 6.2.9 ships 4.1.1).
- Keep a single `GamingWidget.xaml`. Splitting into per-tab UserControls would
  move ~660 named elements (2600 code-behind references) and 238 handlers out
  of the Page and break upstream merges of the code-behind too.
- Top tabs stay `RadioButton`s styled like a segmented pill: the drift guard in
  `Features/Navigation/GamingWidget.Navigation.cs` depends on them. Active tab
  shows icon + label, inactive tabs icon only.
- Legion tab: sub-sections with `Segmented` (Controller, Gyro, Input, Lighting,
  Drivers).
- Rows: transparent `SettingsCard` (min 56 px) inside the existing cards. A tap
  anywhere on a toggle row flips it (`ToggleRow_Tapped`); gamepad focus stays on
  the ToggleSwitch because the Performance focus chain and GotFocus hooks use it.
- 2-5 choices: `Segmented` with `SingleSelectionFollowsFocus="False"` (D-pad
  moves, A applies). `TDPModeComboBox` and `OSPowerModeComboBox` keep their
  names but are `Segmented`; the Performance focus chain targets their selected
  item (`SegmentFocusTarget`).
- Sliders: implicit style adds `IsFocusEngagementEnabled` to unstyled sliders.
- Quick tiles: still built in code (marquee, edit mode, combo binding). On/off
  tiles fill with the accent (`tileOnBrush`), multi-value tiles tint icon + bar
  with the severity color, action tiles have no bar (`ApplyTileFillVisual`).
- Quick metrics: one cell per component (Battery, CPU, GPU, Memory), built from
  the existing metric selection and order; VRAM is the helper's `vramUsed` (MB,
  LibreHardwareMonitor `GPUMemoryUsed`).
- Localization: `Strings/en-US` and `Strings/it-IT` `Resources.resw`.
  XAML text uses `x:Uid` (resource names are case-insensitive); code strings use
  `LocalizedString(key, fallback)`.
- Bottom command bar with gamepad key hints; LB/RB only on the Legion tab.

## Progress

- [x] Toolkit packages build
- [x] Shared styles, safe sliders, command bar, type scale (9-11 px → 12)
- [x] Top tabs restyle with icons
- [x] Toggle/combo rows as SettingsCard (35 converted by pattern)
- [x] TDP Mode and OS Power Mode as Segmented
- [x] Legion sub-sections (Controller, VIIPER, Input, Lighting, Device)
- [x] Quick tile fill, combined metrics, VRAM
- [x] Localization of XAML text, tile names/states, metric headers
- [x] Collapsible card headers: whole header tappable (`ExpandHeader_Tapped`
  drives the existing expand button through its automation peer), expand
  buttons at least 44x40. Kept instead of `SettingsExpander` to reuse the
  existing expand handlers.
- [x] Small buttons enlarged; delete-profile emoji replaced by the Delete glyph
- [x] Literal UI strings set in code use `LocalizedString` (`Code_*` keys)
- [x] Interpolated UI strings in code use
  `string.Format(LocalizedString("CodeF_*", ...))`; two "Scope: ..." strings
  with inline ternaries stay English
- [x] Profile list: type scale and 32 px game icons; global summary stays a table
- [ ] Device checklist (Game Bar, touch, controller, Xbox FSE)
