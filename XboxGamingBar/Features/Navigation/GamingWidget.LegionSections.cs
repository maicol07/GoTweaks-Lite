using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        /// <summary>Legion sub-section containers, in the same order as LegionSectionSelector's items.</summary>
        private UIElement[] LegionSections => new UIElement[]
        {
            LegionControllerSection, LegionViiperSection, LegionInputSection, LegionLightingSection, LegionDeviceSection,
        };

        private void LegionSectionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Fires once from InitializeComponent (SelectedIndex="0") before the containers exist.
            if (LegionDeviceSection == null) return;
            var sections = LegionSections;
            for (int i = 0; i < sections.Length; i++)
            {
                sections[i].Visibility = i == LegionSectionSelector.SelectedIndex ? Visibility.Visible : Visibility.Collapsed;
            }
            LegionScrollViewer.ChangeView(null, 0, null, true);
        }

        /// <summary>Selects the Legion sub-section that contains <paramref name="section"/>.</summary>
        internal void ShowLegionSection(UIElement section)
        {
            int index = Array.IndexOf(LegionSections, section);
            if (index >= 0) LegionSectionSelector.SelectedIndex = index;
        }

        /// <summary>LB/RB: previous/next Legion sub-section, wrapping around.</summary>
        private void CycleLegionSection(int delta)
        {
            int count = LegionSections.Length;
            LegionSectionSelector.SelectedIndex = (LegionSectionSelector.SelectedIndex + delta + count) % count;
        }
    }
}
