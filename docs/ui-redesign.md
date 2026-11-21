# Widget UI redesign (touch + gamepad)

Target: Legion Go 2 inside Xbox Game Bar. Mockups:
https://claude.ai/artifact/3dj9prEoPe1GUJkUEcDcn5

## Decisions

- Stay on UWP + WinUI 2.8. WinUI 3 cannot host a Game Bar widget.
- CommunityToolkit 8 UWP controls are used as they are: `SettingsCard`,
  `SettingsExpander`, `Segmented`. No custom styles or templates emulate them;
  only the toolkit/WinUI theme brushes are overridden at page level to match
  the widget palette. Release x64 .NET Native builds; MSB3277 on
  System.Security.* 4.1.1 vs 5.0 is expected (toolkit references 5.0, UWP 6.2.9
  ships 4.1.1).
- Keep a single `GamingWidget.xaml`. Splitting into per-tab UserControls would
  move ~660 named elements (2600 code-behind references) and 238 handlers out
  of the Page and break upstream merges of the code-behind too.
- Tabs: the main bar and the Legion sub-sections are `Segmented`. Only the
  selected item shows its label (`UpdateSegmentLabels`; a `StackPanel` items
  panel lets it grow, and it forces the `IconOnLeft` state because toolkit
  8.2.251219 goes to a non-existent `IconLeft` state). The main bar does not
  select on focus (A or a tap selects), LT/RT switch tabs, LB/RB switch Legion
  sub-sections.
- Settings: every setting is a `SettingsCard`; groups and collapsible sections
  are `SettingsExpander` with their content in `Items`. Custom content (sliders
  with labels, graphs, pickers) sits in a `SettingsCard` with
  `ContentAlignment="Left"` or `"Vertical"`. Logic that used to run on an expand
  button (load hotkeys, refresh remap UI, init fan graphs, stick preview) runs on
  the expander's `Expanded`/`Collapsed` events.
- Each row in `Items` is its own `SettingsCard`. A panel the code shows or hides
  as a group may sit in `Items` but needs `Style="{x:Null}"` (the expander puts
  its SettingsCard style on every item and crashes otherwise); its cards get
  that style from `ApplyExpanderItemStyles`. No expander nested inside a card:
  color rows use a `DropDownButton` with the `ColorPicker` in its flyout.
- Toggle rows also flip on a tap anywhere on the card (`ToggleRow_Tapped`).
- 2-5 choices: `Segmented` with `SingleSelectionFollowsFocus="False"` (D-pad
  moves, A applies). `TDPModeComboBox` and `OSPowerModeComboBox` keep their
  names but are `Segmented`; the Performance focus chain targets their selected
  item (`SegmentFocusTarget`).
- Sliders: implicit style adds `IsFocusEngagementEnabled` to unstyled sliders.
- Quick tiles: built in code (marquee, edit mode, combo binding); on/off tiles
  fill with the accent, multi-value tiles tint icon + bar with the severity
  color, action tiles have no bar (`ApplyTileFillVisual`).
- Quick metrics: one cell per component (Battery, CPU, GPU, Memory), on by
  default; VRAM is the helper's `vramUsed` (MB, LibreHardwareMonitor
  `GPUMemoryUsed`).
- Notices use WinUI `InfoBar`.
- Localization: `Strings/en-US` and `Strings/it-IT` `Resources.resw`.
  XAML text uses `x:Uid` (resource names are case-insensitive); code strings use
  `LocalizedString(key, fallback)`.
- Bottom command bar with gamepad key hints; LB/RB only on the Legion tab.

## Open

- Device checklist on the Legion Go 2: Game Bar, touch, controller, Xbox FSE.
- Two "Scope: ..." strings with inline ternaries stay English.
