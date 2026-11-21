using CommunityToolkit.WinUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        /// <summary>
        /// Tapped handler for SettingsCard rows that host a ToggleSwitch: a tap anywhere on the
        /// row flips the switch, so the whole row is one touch target. Gamepad focus stays on the
        /// switch itself (the Performance focus chain and GotFocus hooks rely on it). Taps that
        /// land on the switch are left to the switch; disabled switches are left alone.
        /// </summary>
        private void ToggleRow_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(sender is SettingsCard card) || !(card.Content is ToggleSwitch toggle) || !toggle.IsEnabled)
            {
                return;
            }
            for (var node = e.OriginalSource as DependencyObject; node != null && node != card; node = VisualTreeHelper.GetParent(node))
            {
                if (node == toggle) return;
            }
            toggle.IsOn = !toggle.IsOn;
        }

        /// <summary>
        /// Tapped handler for collapsible card headers: a tap anywhere on the header acts like
        /// the header's own expand button (whose Click handler shows/hides the content), so the
        /// whole header is one touch target. Taps on the button itself are left to the button.
        /// </summary>
        private void ExpandHeader_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(sender is Panel header)) return;
            var button = FindExpandButton(header);
            if (button == null || !button.IsEnabled) return;
            for (var node = e.OriginalSource as DependencyObject; node != null && node != header; node = VisualTreeHelper.GetParent(node))
            {
                if (node == button) return;
            }
            if (button is ToggleButton toggle)
            {
                new ToggleButtonAutomationPeer(toggle).Toggle();
            }
            else if (button is Button plain)
            {
                new ButtonAutomationPeer(plain).Invoke();
            }
        }

        /// <summary>
        /// The header's expand button: a direct child, inside a nested panel (e.g. next to a color
        /// swatch) or as the content of a SettingsCard header row.
        /// </summary>
        private static ButtonBase FindExpandButton(Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is ButtonBase b && b.Name.Contains("Expand")) return b;
                if (child is Panel nested && FindExpandButton(nested) is ButtonBase found) return found;
                if (child is ContentControl host && host.Content is ButtonBase content && content.Name.Contains("Expand")) return content;
            }
            return null;
        }
    }
}
