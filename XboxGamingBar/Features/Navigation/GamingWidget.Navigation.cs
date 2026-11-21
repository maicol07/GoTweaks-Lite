using Microsoft.Gaming.XboxGameBar;
using Microsoft.Gaming.XboxGameBar.Input;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using NLog;
using Shared.Data;
using Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.System.Power;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Input;
using System.Runtime.InteropServices;
using Windows.UI;
using XboxGamingBar.Data;
using XboxGamingBar.Event;
using XboxGamingBar.IPC;
using XboxGamingBar.QuickSettings;
using Shared.Enums;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {

        // Tag of the selected main tab (Quick, Performance, Game, Legion, GPD, System).
        private string lastUserNavTag = "Quick";

        /// <summary>
        /// A (or Enter/Space) on a focused tab selects it and moves focus into the tab's content.
        /// The Segmented does not select on focus (SingleSelectionFollowsFocus=False), so
        /// focus restoration can't switch tabs by itself.
        /// </summary>
        internal void AttachNavInteractionTracking()
        {
            MainNavPanel.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((s, e) =>
            {
                if (e.Key == VirtualKey.GamepadA || e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
                {
                    if (FocusManager.GetFocusedElement() is SegmentedItem focused) focused.IsSelected = true;
                    var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Low, FocusFirstControlInActiveTab);
                }
            }), handledEventsToo: true);
        }

        /// <summary>Icon-only tabs, with the label (AutomationProperties.Name) on the selected one.</summary>
        private static void UpdateSegmentLabels(ListViewBase segmented)
        {
            foreach (var item in segmented.Items.OfType<SegmentedItem>())
            {
                item.Content = item.IsSelected ? Windows.UI.Xaml.Automation.AutomationProperties.GetName(item) : null;
            }
        }

        private void MainNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainNavPanel.SelectedItem is SegmentedItem selectedItem)
            {
                UpdateSegmentLabels(MainNavPanel);
                string tag = selectedItem.Tag?.ToString() ?? "";
                lastUserNavTag = tag;

                // Hide all sections
                QuickSettingsScrollViewer.Visibility = Visibility.Collapsed;
                PerformanceScrollViewer.Visibility = Visibility.Collapsed;
                GameScrollViewer.Visibility = Visibility.Collapsed;
                LegionScrollViewer.Visibility = Visibility.Collapsed;
                GPDScrollViewer.Visibility = Visibility.Collapsed;
                SystemScrollViewer.Visibility = Visibility.Collapsed;

                // Stop fan curve updates when leaving Legion tab (will be re-enabled if Legion is selected)
                legionFanCurveVisible?.SetVisible(false);

                // LB/RB hint only applies to the Legion sub-sections.
                CommandHintSections.Visibility = tag == "Legion" ? Visibility.Visible : Visibility.Collapsed;

                // Show selected section and scroll to top
                switch (tag)
                {
                    case "Quick":
                        QuickSettingsScrollViewer.Visibility = Visibility.Visible;
                        QuickSettingsScrollViewer.ChangeView(null, 0, null, true);
                        UpdateQuickSettingsTileStates();
                        break;
                    case "Performance":
                        PerformanceScrollViewer.Visibility = Visibility.Visible;
                        PerformanceScrollViewer.ChangeView(null, 0, null, true);
                        SchedulePerformanceFocusChainRebuild();
                        break;
                    case "Game":
                        GameScrollViewer.Visibility = Visibility.Visible;
                        GameScrollViewer.ChangeView(null, 0, null, true);
                        break;
                    case "Legion":
                        LegionScrollViewer.Visibility = Visibility.Visible;
                        LegionScrollViewer.ChangeView(null, 0, null, true);
                        // Update fan curve visibility when switching to Legion tab
                        legionFanCurveVisible?.SetVisible(isFanCurveExpanded);
                        // Force remap UI refresh when Legion tab becomes active.
                        RefreshLegionEnhancedRemapUi();
                        _ = UpdateDAServiceAsync(null);
                        break;
                    case "GPD":
                        GPDScrollViewer.Visibility = Visibility.Visible;
                        GPDScrollViewer.ChangeView(null, 0, null, true);
                        break;
                    case "System":
                        SystemScrollViewer.Visibility = Visibility.Visible;
                        SystemScrollViewer.ChangeView(null, 0, null, true);
                        break;
                }
            }
        }

        // Trigger-press edge tracking for tab navigation. Holding LT/RT would otherwise
        // auto-repeat (WasKeyDown=true) and cycle tabs continuously. We require the user
        // to release the trigger before accepting another press, and also apply a small
        // minimum interval as a belt-and-suspenders debounce.
        private bool ltTriggerHeld;
        private bool rtTriggerHeld;
        private DateTime lastTriggerNavigateUtc = DateTime.MinValue;
        private static readonly TimeSpan TriggerNavigateDebounce = TimeSpan.FromMilliseconds(150);

        private void GamingWidget_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle LT (Left Trigger) and RT (Right Trigger) for tab navigation.
            // Using PreviewKeyDown to intercept before ScrollViewer handles it.
            // Skip when the key is auto-repeating while still held — only the initial
            // press-edge should advance a tab. One press == one tab.
            if (e.Key == VirtualKey.GamepadLeftTrigger)
            {
                if (!ltTriggerHeld && !e.KeyStatus.WasKeyDown
                    && (DateTime.UtcNow - lastTriggerNavigateUtc) >= TriggerNavigateDebounce)
                {
                    ltTriggerHeld = true;
                    lastTriggerNavigateUtc = DateTime.UtcNow;
                    NavigateToPreviousTab();
                }
                e.Handled = true;
                return;
            }
            else if (e.Key == VirtualKey.GamepadRightTrigger)
            {
                if (!rtTriggerHeld && !e.KeyStatus.WasKeyDown
                    && (DateTime.UtcNow - lastTriggerNavigateUtc) >= TriggerNavigateDebounce)
                {
                    rtTriggerHeld = true;
                    lastTriggerNavigateUtc = DateTime.UtcNow;
                    NavigateToNextTab();
                }
                e.Handled = true;
                return;
            }
            // LB/RB cycle the Legion tab's sub-sections (one press == one section).
            else if ((e.Key == VirtualKey.GamepadLeftShoulder || e.Key == VirtualKey.GamepadRightShoulder)
                     && lastUserNavTag == "Legion")
            {
                if (!e.KeyStatus.WasKeyDown)
                {
                    CycleLegionSection(e.Key == VirtualKey.GamepadRightShoulder ? 1 : -1);
                }
                e.Handled = true;
                return;
            }
            // Handle D-pad down from nav items to focus content area (for overflow menu items)
            else if (e.Key == VirtualKey.GamepadDPadDown)
            {
                var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
                // Check if focus is on a nav RadioButton or within the nav area
                if (focusedElement != null && IsInNavigationArea(focusedElement))
                {
                    // Mark as handled immediately to prevent default XY navigation
                    e.Handled = true;

                    // Use TryMoveFocus to move to the first focusable element downward
                    FocusManager.TryMoveFocus(FocusNavigationDirection.Down);
                }
            }
        }

        private void GamingWidget_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            // Clear the press-edge state so the next LT/RT press advances exactly one
            // tab. Without this, auto-repeat during a held trigger would cycle through
            // the entire tab strip.
            if (e.Key == VirtualKey.GamepadLeftTrigger)
            {
                ltTriggerHeld = false;
            }
            else if (e.Key == VirtualKey.GamepadRightTrigger)
            {
                rtTriggerHeld = false;
            }
        }

        /// <summary>
        /// Forcibly clears any held LT/RT press-edge state. Called when the widget gains
        /// focus. A controller re-enumeration (for example switching X-Input/D-Input) can
        /// leave the OS believing RT/LT is stuck-down (no KeyUp arrives because the device
        /// disappeared between events), which would otherwise leave tab nav wedged until
        /// the user gets a fresh KeyUp. Resetting here lets the very next physical press
        /// act as a clean press-edge.
        /// </summary>
        internal void ResetTriggerTabNavState()
        {
            if (ltTriggerHeld || rtTriggerHeld)
            {
                Logger.Info("Clearing stuck LT/RT tab-nav state (focus transition)");
            }
            ltTriggerHeld = false;
            rtTriggerHeld = false;
            lastTriggerNavigateUtc = DateTime.MinValue;
        }

        private bool IsInNavigationArea(FrameworkElement element)
        {
            // Check if any of our nav items has focus
            // This works regardless of whether the item is in the main bar or overflow menu
            if (QuickNavItem.FocusState != FocusState.Unfocused) return true;
            if (PerformanceNavItem.FocusState != FocusState.Unfocused) return true;
            if (ProfilesNavItem.FocusState != FocusState.Unfocused) return true;
            if (LegionNavItem.FocusState != FocusState.Unfocused) return true;
            if (GPDNavItem.FocusState != FocusState.Unfocused) return true;
            if (SystemNavItem.FocusState != FocusState.Unfocused) return true;

            // Fallback: walk visual tree for other nav-related elements
            var current = element;
            while (current != null)
            {
                // Check if we're in the nav panel
                if (current == MainNavPanel)
                    return true;
                current = VisualTreeHelper.GetParent(current) as FrameworkElement;
            }
            return false;
        }

        private void NavigateToPreviousTab()
        {
            var visibleItems = GetVisibleNavigationItems();
            if (visibleItems.Count == 0) return;

            // Find currently checked item
            var currentItem = visibleItems.FirstOrDefault(rb => rb.IsSelected);
            int currentIndex = currentItem != null ? visibleItems.IndexOf(currentItem) : 0;

            var target = currentIndex > 0 ? visibleItems[currentIndex - 1] : visibleItems[visibleItems.Count - 1];
            target.IsSelected = true;
        }

        private void NavigateToNextTab()
        {
            var visibleItems = GetVisibleNavigationItems();
            if (visibleItems.Count == 0) return;

            // Find currently checked item
            var currentItem = visibleItems.FirstOrDefault(rb => rb.IsSelected);
            int currentIndex = currentItem != null ? visibleItems.IndexOf(currentItem) : 0;

            var target = currentIndex < visibleItems.Count - 1 ? visibleItems[currentIndex + 1] : visibleItems[0];
            target.IsSelected = true;
        }

        private List<SegmentedItem> GetVisibleNavigationItems()
        {
            return MainNavPanel.Items.OfType<SegmentedItem>().Where(i => i.Visibility == Visibility.Visible).ToList();
        }

        /// <summary>
        /// The ScrollViewer of the currently active tab (matches lastUserNavTag). Null if unknown.
        /// </summary>
        private ScrollViewer GetActiveTabScrollViewer()
        {
            switch (lastUserNavTag)
            {
                case "Quick": return QuickSettingsScrollViewer;
                case "Performance": return PerformanceScrollViewer;
                case "Game": return GameScrollViewer;
                case "Legion": return LegionScrollViewer;
                case "GPD": return GPDScrollViewer;
                case "System": return SystemScrollViewer;
                default: return null;
            }
        }

        /// <summary>
        /// Moves gamepad focus onto the first focusable control inside the active tab's content.
        /// Called when the user commits to a tab (A/Enter on the nav item) so they don't have to
        /// D-pad down from the tab strip every time. No-op if the tab has no focusable content.
        /// </summary>
        internal void FocusFirstControlInActiveTab()
        {
            try
            {
                var sv = GetActiveTabScrollViewer();
                if (sv == null || sv.Visibility != Visibility.Visible) return;
                if (FocusManager.FindFirstFocusableElement(sv) is Control first)
                {
                    first.Focus(FocusState.Programmatic);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"FocusFirstControlInActiveTab failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Puts gamepad focus on the currently active tab in the nav strip. Called when the widget
        /// becomes visible so the pad can navigate immediately without the user first touching the
        /// screen. Focuses (doesn't re-check) the already-selected tab, so it can't cause nav drift.
        /// </summary>
        internal void FocusActiveNavItem()
        {
            try
            {
                var active = MainNavPanel.Items.OfType<SegmentedItem>()
                    .FirstOrDefault(rb => rb.IsSelected && rb.Visibility == Visibility.Visible);
                active?.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                Logger.Warn($"FocusActiveNavItem failed: {ex.Message}");
            }
        }

    }
}
