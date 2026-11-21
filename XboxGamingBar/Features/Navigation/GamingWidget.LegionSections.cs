using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        /// <summary>Legion sub-section containers, in the same order as the LegionSectionTabs items.</summary>
        private UIElement[] LegionSections => new UIElement[]
        {
            LegionControllerSection, LegionViiperSection, LegionInputSection, LegionLightingSection, LegionDeviceSection,
        };

        private void LegionSectionTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSegmentLabels(LegionSectionTabs);
            // Fires once from InitializeComponent (IsSelected="True") before the containers exist.
            if (LegionDeviceSection == null) return;
            int index = LegionSectionTabs.SelectedIndex;
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
            if (index >= 0) LegionSectionTabs.SelectedIndex = index;
        }

        /// <summary>LB/RB: previous/next Legion sub-section, wrapping around.</summary>
        private void CycleLegionSection(int delta)
        {
            int count = LegionSectionTabs.Items.Count;
            LegionSectionTabs.SelectedIndex = (LegionSectionTabs.SelectedIndex + delta + count) % count;
        }
    }
}
