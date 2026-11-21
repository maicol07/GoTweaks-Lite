using CommunityToolkit.WinUI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        /// SettingsExpander styles the SettingsCards that are its direct Items. Rows that sit inside a
        /// panel the code shows or hides as a group are one level deeper, so they get the same style
        /// from the expander's own ItemContainerStyleSelector.
        /// </summary>
        private static void ApplyExpanderItemStyles(object node)
        {
            switch (node)
            {
                case SettingsExpander expander:
                    ApplyExpanderItemStyles(expander.Header);
                    ApplyExpanderItemStyles(expander.Content);
                    foreach (var item in expander.Items)
                    {
                        if (item is Panel group) StyleNestedRows(expander, group);
                        ApplyExpanderItemStyles(item);
                    }
                    break;
                case Panel panel:
                    foreach (var child in panel.Children) ApplyExpanderItemStyles(child);
                    break;
                case ContentControl control:
                    ApplyExpanderItemStyles(control.Content);
                    break;
                case Border border:
                    ApplyExpanderItemStyles(border.Child);
                    break;
            }
        }

        private static void StyleNestedRows(SettingsExpander expander, Panel group)
        {
            foreach (var child in group.Children)
            {
                if (child is SettingsCard card && card.ReadLocalValue(StyleProperty) == DependencyProperty.UnsetValue)
                {
                    card.Style = expander.ItemContainerStyleSelector?.SelectStyle(null, card);
                }
                else if (child is Panel inner)
                {
                    StyleNestedRows(expander, inner);
                }
            }
        }
    }
}
