using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        // Quick metrics are shown as one cell per component. The selected metrics (and their
        // order, from the existing selection/reorder UI) decide which cells appear: the first
        // selected metric of a component is the cell's big value, the others go on the
        // secondary line.
        private enum MetricGroup { Battery, CPU, GPU, Memory }

        private static readonly Dictionary<MetricType, MetricGroup> metricGroups = new Dictionary<MetricType, MetricGroup>
        {
            { MetricType.BatteryLevel, MetricGroup.Battery },
            { MetricType.BatteryDrain, MetricGroup.Battery },
            { MetricType.TimeRemaining, MetricGroup.Battery },
            { MetricType.CPUUsage, MetricGroup.CPU },
            { MetricType.CPUTemp, MetricGroup.CPU },
            { MetricType.CPUWattage, MetricGroup.CPU },
            { MetricType.GPUUsage, MetricGroup.GPU },
            { MetricType.GPUTemp, MetricGroup.GPU },
            { MetricType.GPUWattage, MetricGroup.GPU },
            { MetricType.MemoryUsage, MetricGroup.Memory },
            { MetricType.VRAMUsage, MetricGroup.Memory },
        };

        private static readonly Dictionary<MetricGroup, (string Label, string Glyph)> metricGroupHeaders = new Dictionary<MetricGroup, (string, string)>
        {
            { MetricGroup.Battery, ("Battery", "") },
            { MetricGroup.CPU, ("CPU", ((char)0xEEA1).ToString()) },
            { MetricGroup.GPU, ("GPU", ((char)0xE964).ToString()) },
            { MetricGroup.Memory, ("Memory", ((char)0xEEA0).ToString()) },
        };

        private class MetricCell
        {
            public MetricGroup Group;
            public List<MetricType> Metrics;
            public FontIcon Icon;
            public TextBlock Primary;
            public TextBlock Secondary;
            public ProgressBar Level;
        }

        private readonly List<MetricCell> metricCells = new List<MetricCell>();

        private void RebuildMetricsGrid()
        {
            if (QuickMetricsGrid == null) return;

            QuickMetricsGrid.Children.Clear();
            QuickMetricsGrid.ColumnDefinitions.Clear();
            metricCells.Clear();

            if (selectedMetrics.Count == 0)
            {
                QuickMetricsRow.Visibility = Visibility.Collapsed;
                return;
            }

            foreach (var group in selectedMetrics.Where(metricGroups.ContainsKey).Select(m => metricGroups[m]).Distinct())
            {
                metricCells.Add(new MetricCell { Group = group, Metrics = selectedMetrics.Where(m => metricGroups.TryGetValue(m, out var g) && g == group).ToList() });
            }

            var dividerBrush = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
            var captionBrush = new SolidColorBrush(Color.FromArgb(255, 0xB9, 0xC0, 0xCC));
            for (int i = 0; i < metricCells.Count; i++)
            {
                var cell = metricCells[i];
                QuickMetricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var header = metricGroupHeaders[cell.Group];

                cell.Icon = new FontIcon { Glyph = header.Glyph, FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 16, Foreground = captionBrush };
                var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                headerRow.Children.Add(cell.Icon);
                headerRow.Children.Add(new TextBlock { Text = header.Label, FontSize = 12, Foreground = captionBrush, VerticalAlignment = VerticalAlignment.Center });

                cell.Primary = new TextBlock { Text = "--", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.White), Margin = new Thickness(0, 2, 0, 0) };
                Typography.SetNumeralAlignment(cell.Primary, FontNumeralAlignment.Tabular);
                cell.Secondary = new TextBlock { FontSize = 12, Foreground = captionBrush, TextTrimming = TextTrimming.CharacterEllipsis };
                cell.Level = new ProgressBar { Minimum = 0, Maximum = 100, Height = 3, MinHeight = 3, Margin = new Thickness(0, 5, 0, 0) };

                var panel = new StackPanel { Padding = new Thickness(12, 2, 12, 2) };
                panel.Children.Add(headerRow);
                panel.Children.Add(cell.Primary);
                panel.Children.Add(cell.Secondary);
                panel.Children.Add(cell.Level);

                var host = new Border { Child = panel, BorderBrush = dividerBrush, BorderThickness = new Thickness(i == 0 ? 0 : 1, 0, 0, 0) };
                Grid.SetColumn(host, i);
                QuickMetricsGrid.Children.Add(host);
            }

            if (quickMetricsEnabled)
            {
                QuickMetricsRow.Visibility = Visibility.Visible;
            }

            // Repaint immediately from cached data instead of waiting for the next helper push.
            UpdateMetricsDisplay();
            RebuildMetricsReorderList();
        }

        private void UpdateMetricsDisplay()
        {
            foreach (var cell in metricCells)
            {
                var values = cell.Metrics.Select(FormatMetricValue).ToList();
                cell.Primary.Text = values[0];
                cell.Secondary.Text = string.Join(" · ", values.Skip(1));
                cell.Secondary.Visibility = values.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

                double? level = MetricLevelPercent(cell.Metrics[0]);
                cell.Level.Visibility = level.HasValue ? Visibility.Visible : Visibility.Collapsed;
                if (level.HasValue)
                {
                    var brush = MetricSeverityBrush(cell.Metrics[0]) ?? tileAccentBrush;
                    cell.Level.Value = level.Value;
                    cell.Level.Foreground = brush;
                    cell.Icon.Foreground = MetricSeverityBrush(cell.Metrics[0]) ?? cell.Secondary.Foreground;
                }
            }
        }

        private string FormatMetricValue(MetricType metricType)
        {
            double v;
            switch (metricType)
            {
                case MetricType.BatteryDrain:
                    if (!currentMetricsData.TryGetValue("batteryDrain", out v) || v == 0) return "-- W";
                    return v > 0 ? $"{v:F1} W" : $"+{-v:F1} W";
                case MetricType.BatteryLevel:
                    return currentMetricsData.TryGetValue("batteryLevel", out v) && v >= 0 ? $"{v:F0}%" : "--%";
                case MetricType.CPUUsage:
                    return currentMetricsData.TryGetValue("cpuUsage", out v) && v >= 0 ? $"{v:F0}%" : "--%";
                case MetricType.CPUTemp:
                    return currentMetricsData.TryGetValue("cpuTemp", out v) && v > 0 ? $"{v:F0}°C" : "--°C";
                case MetricType.CPUWattage:
                    return currentMetricsData.TryGetValue("cpuWattage", out v) && v >= 0 ? $"{v:F1} W" : "-- W";
                case MetricType.GPUUsage:
                    return currentMetricsData.TryGetValue("gpuUsage", out v) && v >= 0 ? $"{v:F0}%" : "--%";
                case MetricType.GPUTemp:
                    return currentMetricsData.TryGetValue("gpuTemp", out v) && v > 0 ? $"{v:F0}°C" : "--°C";
                case MetricType.GPUWattage:
                    return currentMetricsData.TryGetValue("gpuWattage", out v) && v >= 0 ? $"{v:F1} W" : "-- W";
                case MetricType.MemoryUsage:
                    return currentMetricsData.TryGetValue("memoryUsage", out v) && v >= 0 ? $"RAM {v:F0}%" : "RAM --%";
                case MetricType.VRAMUsage:
                    return currentMetricsData.TryGetValue("vramUsed", out v) && v >= 0 ? $"VRAM {v / 1024:F1} GB" : "VRAM --";
                case MetricType.TimeRemaining:
                    currentMetricsData.TryGetValue("timeRemaining", out var timeRem);
                    currentMetricsData.TryGetValue("timeToFull", out var timeFull);
                    double seconds = currentMetricsIsCharging ? timeFull : timeRem;
                    if (seconds <= 0) return "--:--";
                    return $"{(int)(seconds / 3600)}:{(int)(seconds % 3600 / 60):D2}";
                default:
                    return "--";
            }
        }

        /// <summary>0-100 fill for the cell's level bar, or null when the metric has no natural range.</summary>
        private double? MetricLevelPercent(MetricType metricType)
        {
            string key;
            switch (metricType)
            {
                case MetricType.BatteryLevel: key = "batteryLevel"; break;
                case MetricType.CPUUsage: key = "cpuUsage"; break;
                case MetricType.GPUUsage: key = "gpuUsage"; break;
                case MetricType.MemoryUsage: key = "memoryUsage"; break;
                case MetricType.CPUTemp: key = "cpuTemp"; break;
                case MetricType.GPUTemp: key = "gpuTemp"; break;
                default: return null;
            }
            if (!currentMetricsData.TryGetValue(key, out var v) || v < 0) return 0;
            // ponytail: temperatures map 40-95 °C onto the bar; tune if the device runs hotter or cooler.
            bool isTemp = metricType == MetricType.CPUTemp || metricType == MetricType.GPUTemp;
            return isTemp ? Math.Max(0, Math.Min(100, (v - 40) / 55 * 100)) : Math.Min(100, v);
        }

        /// <summary>Severity color for battery level and temperatures; null means "use the accent".</summary>
        private SolidColorBrush MetricSeverityBrush(MetricType metricType)
        {
            double v;
            switch (metricType)
            {
                case MetricType.BatteryLevel:
                    if (currentMetricsIsCharging) return tileSeverityGreenBrush;
                    if (!currentMetricsData.TryGetValue("batteryLevel", out v) || v < 0) return null;
                    return v <= 20 ? tileSeverityRedBrush : v <= 50 ? tileSeverityOrangeBrush : tileSeverityGreenBrush;
                case MetricType.CPUTemp:
                case MetricType.GPUTemp:
                    if (!currentMetricsData.TryGetValue(metricType == MetricType.CPUTemp ? "cpuTemp" : "gpuTemp", out v) || v <= 0) return null;
                    return v >= 90 ? tileSeverityRedBrush : v >= 75 ? tileSeverityOrangeBrush : tileSeverityGreenBrush;
                default:
                    return null;
            }
        }
    }
}
