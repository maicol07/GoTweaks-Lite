using System;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace XboxGamingBar
{
    /// <summary>
    /// Slides an accent "pill" behind the checked RadioButton of a tab strip. The pill is a
    /// sibling drawn under the tabs panel; its X offset and width are animated to the checked
    /// tab once layout has settled (the checked tab grows when its label appears).
    /// </summary>
    internal sealed class PillTabIndicator
    {
        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(250);
        private readonly Panel tabs;
        private readonly FrameworkElement pill;
        private readonly TranslateTransform transform;
        private RadioButton current;

        public PillTabIndicator(Panel tabs, FrameworkElement pill, TranslateTransform transform)
        {
            this.tabs = tabs;
            this.pill = pill;
            this.transform = transform;
            foreach (var child in tabs.Children)
            {
                if (child is RadioButton tab)
                {
                    tab.Checked += (s, e) => MoveTo((RadioButton)s, true);
                    if (tab.IsChecked == true) current = tab;
                }
            }
            tabs.SizeChanged += (s, e) => MoveTo(current, false);
        }

        private void MoveTo(RadioButton target, bool animate)
        {
            if (target == null) return;
            current = target;
            _ = tabs.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (current != target || target.ActualWidth <= 0) return;
                double x = target.TransformToVisual(tabs).TransformPoint(new Point(0, 0)).X;
                double width = target.ActualWidth;
                if (!animate || !(pill.Width > 0))
                {
                    transform.X = x;
                    pill.Width = width;
                    return;
                }
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                var move = new DoubleAnimation { To = x, Duration = Duration, EasingFunction = ease };
                Storyboard.SetTarget(move, transform);
                Storyboard.SetTargetProperty(move, "X");
                var resize = new DoubleAnimation { To = width, Duration = Duration, EasingFunction = ease, EnableDependentAnimation = true };
                Storyboard.SetTarget(resize, pill);
                Storyboard.SetTargetProperty(resize, "Width");
                var storyboard = new Storyboard();
                storyboard.Children.Add(move);
                storyboard.Children.Add(resize);
                storyboard.Begin();
            });
        }
    }
}
