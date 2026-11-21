using System;
using System.Collections.Generic;
using CommunityToolkit.WinUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        /// <summary>
        /// Collapsible sections are SettingsExpanders whose Tag names the section's original expand
        /// button. That button stays in the tree (hidden) and its Click handler still shows/hides the
        /// section content and loads data on first open, so expanding or collapsing the expander just
        /// runs that handler.
        /// </summary>
        private Dictionary<string, RoutedEventHandler> LegacyExpandHandlers => new Dictionary<string, RoutedEventHandler>
        {
            { "AboutExpandButton", AboutExpandButton_Click },
            { "ButtonRemappingExpandToggle", ButtonRemappingExpandToggle_Click },
            { "DebugExpandButton", DebugExpandButton_Click },
            { "DriverUpdatesExpandToggle", DriverUpdatesExpandToggle_Click },
            { "GPDButtonRemapExpandToggle", GPDButtonRemapExpandToggle_Click },
            { "GyroSettingsExpandToggle", GyroSettingsExpandToggle_Click },
            { "HotkeysExpandButton", HotkeysExpandButton_Click },
            { "LightingExpandToggle", LightingExpandToggle_Click },
            { "OSDCustomizeExpandButton", OSDCustomizeExpandButton_Click },
            { "PowerAndSleepExpandButton", PowerAndSleepExpandButton_Click },
            { "ProfileDetectionExpandToggle", ProfileDetectionExpandToggle_Click },
            { "ProfileSettingsExpandToggle", ProfileSettingsExpandToggle_Click },
            { "SpecialRemappingExpandButton", SpecialRemappingExpandButton_Click },
            { "StickDeadzonesExpandToggle", StickDeadzonesExpandToggle_Click },
            { "TouchpadVibrationExpandToggle", TouchpadVibrationExpandToggle_Click },
        };

        private void LegacyExpander_Toggled(object sender, EventArgs e)
        {
            if (!(sender is SettingsExpander expander) || !(expander.Tag is string name)) return;
            if (LegacyExpandHandlers.TryGetValue(name, out var handler) && FindName(name) is ButtonBase button)
            {
                if (button is ToggleButton toggle) toggle.IsChecked = expander.IsExpanded;
                handler(button, new RoutedEventArgs());
            }
        }
    }
}
