using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        /// <summary>Legion sub-section containers, in the same order as the LegionSectionTabs radio buttons.</summary>
        private UIElement[] LegionSections => new UIElement[]
        {
            LegionControllerSection, LegionViiperSection, LegionInputSection, LegionLightingSection, LegionDeviceSection,
        };

        private RadioButton[] LegionSectionTabButtons => LegionSectionTabs.Children.OfType<RadioButton>().ToArray();

        private void LegionSectionTab_Checked(object sender, RoutedEventArgs e)
        {
            // Fires once from InitializeComponent (IsChecked="True") before the containers exist.
            if (LegionDeviceSection == null) return;
            int index = Array.IndexOf(LegionSectionTabButtons, sender);
            var sections = LegionSections;
            for (int i = 0; i < sections.Length; i++)
            {
                sections[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;
            }
            LegionScrollViewer.ChangeView(null, 0, null, true);
        }

        /// <summary>Selects the Legion sub-section that contains <paramref name="section"/>.</summary>
        internal void ShowLegionSection(UIElement section)
        {
            int index = Array.IndexOf(LegionSections, section);
            if (index >= 0) LegionSectionTabButtons[index].IsChecked = true;
        }

        /// <summary>LB/RB: previous/next Legion sub-section, wrapping around.</summary>
        private void CycleLegionSection(int delta)
        {
            var tabs = LegionSectionTabButtons;
            int current = Array.FindIndex(tabs, t => t.IsChecked == true);
            tabs[(current + delta + tabs.Length) % tabs.Length].IsChecked = true;
        }
    }
}
