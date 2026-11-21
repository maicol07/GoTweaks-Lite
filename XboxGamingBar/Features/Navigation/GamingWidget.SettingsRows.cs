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

    }
}
