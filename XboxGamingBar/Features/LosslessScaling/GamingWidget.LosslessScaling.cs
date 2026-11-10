using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Enums;
using Shared.IPC;
using Windows.Data.Json;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        private JsonArray losslessProfiles;
        private string losslessProfileRevision;
        private readonly Dictionary<string, (Control Editor, string Original)> losslessEditors =
            new Dictionary<string, (Control, string)>();

        private async void OpenLosslessApp_Click(object sender, RoutedEventArgs e)
        {
            if (!await Windows.System.Launcher.LaunchUriAsync(new Uri("steam://rungameid/993090")))
                LosslessProfilesStatus.Text = "Could not open Steam. Launch Lossless Scaling manually.";
        }

        private void ToggleLosslessScaling_Click(object sender, RoutedEventArgs e)
        {
            if (losslessScalingInstalled?.Value == true)
                ToggleLosslessScaling();
            else
                LosslessProfilesStatus.Text = "Lossless Scaling installation was not detected.";
        }

        private async void LoadLosslessProfiles_Click(object sender, RoutedEventArgs e) =>
            await RequestLosslessProfiles(Command.Get, string.Empty, 0);

        private async void SaveLosslessProfile_Click(object sender, RoutedEventArgs e)
        {
            var fields = new JsonObject();
            foreach (var entry in losslessEditors)
            {
                var editor = entry.Value.Editor;
                string value = editor is TextBox text ? text.Text.Trim() : ((ComboBox)editor).SelectedItem as string;
                if (value != null && value != entry.Value.Original)
                    fields[entry.Key] = JsonValue.CreateStringValue(value);
            }

            if (fields.Count == 0)
            {
                LosslessProfilesStatus.Text = "No changes to save.";
                return;
            }

            int index = LosslessProfileSelector.SelectedIndex;
            var payload = new JsonObject
            {
                ["revision"] = JsonValue.CreateStringValue(losslessProfileRevision),
                ["index"] = JsonValue.CreateNumberValue(index),
                ["fields"] = fields,
            };
            await RequestLosslessProfiles(Command.Set, payload.Stringify(), index);
        }

        private async Task RequestLosslessProfiles(Command command, string content, int index)
        {
            LoadLosslessProfilesButton.IsEnabled = false;
            SaveLosslessProfileButton.IsEnabled = false;
            LosslessProfileSelector.IsEnabled = false;
            LosslessProfileFields.IsHitTestVisible = false;
            try
            {
                var response = await App.SendMessageAsync(new ValueSet
                {
                    [nameof(PipeMessage.Command)] = (int)command,
                    [nameof(PipeMessage.Function)] = (int)Function.LosslessScalingProfiles,
                    [nameof(PipeMessage.Content)] = content,
                });
                if (response != null && response.TryGetValue("Error", out var error))
                {
                    LosslessProfilesStatus.Text = error.ToString();
                    return;
                }
                if (response == null || !response.TryGetValue(nameof(PipeMessage.Content), out var value) ||
                    !JsonObject.TryParse(value as string ?? string.Empty, out var payload))
                {
                    LosslessProfilesStatus.Text = "Lossless Scaling profiles could not be loaded from the helper.";
                    return;
                }

                losslessProfileRevision = payload.GetNamedString("revision");
                losslessProfiles = payload.GetNamedArray("profiles");
                LosslessProfileSelector.Items.Clear();
                foreach (var profile in losslessProfiles)
                    LosslessProfileSelector.Items.Add(profile.GetObject().GetNamedString("title"));
                LosslessProfileSelector.Visibility = losslessProfiles.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                LosslessProfileSelector.SelectedIndex = losslessProfiles.Count > 0 ? Math.Min(index, losslessProfiles.Count - 1) : -1;

                string version = payload.GetNamedString("version", "");
                string prefix = string.IsNullOrWhiteSpace(version) ? "" : $"Version {version}. ";
                LosslessProfilesStatus.Text = prefix + (losslessProfiles.Count == 0 ? "Create a profile in Lossless Scaling first."
                    : command == Command.Set ? "Profile saved. Open Lossless Scaling to load it."
                    : "Exit Lossless Scaling from its tray icon before saving.");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Lossless Scaling profile request failed.");
                LosslessProfilesStatus.Text = "Lossless Scaling profile request failed.";
            }
            finally
            {
                LoadLosslessProfilesButton.IsEnabled = true;
                SaveLosslessProfileButton.IsEnabled = true;
                LosslessProfileSelector.IsEnabled = true;
                LosslessProfileFields.IsHitTestVisible = true;
            }
        }

        private void LosslessProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LosslessProfileFields.Children.Clear();
            losslessEditors.Clear();
            int index = LosslessProfileSelector.SelectedIndex;
            if (index >= 0 && losslessProfiles != null && index < losslessProfiles.Count)
            {
                foreach (var item in losslessProfiles[index].GetObject().GetNamedArray("fields"))
                {
                    var field = item.GetObject();
                    string key = field.GetNamedString("key");
                    string label = field.GetNamedString("label");
                    string value = field.GetNamedString("value");
                    string choices = field.GetNamedString("choices");
                    Control editor;
                    if (choices.Length == 0)
                    {
                        editor = new TextBox { Header = label, Text = value };
                    }
                    else
                    {
                        var combo = new ComboBox { Header = label, HorizontalAlignment = HorizontalAlignment.Stretch };
                        foreach (string choice in choices.Split('|')) combo.Items.Add(choice);
                        if (!combo.Items.Contains(value)) combo.Items.Insert(0, value);
                        combo.SelectedItem = value;
                        editor = combo;
                    }
                    LosslessProfileFields.Children.Add(editor);
                    losslessEditors.Add(key, (editor, value));
                }
            }
            SaveLosslessProfileButton.Visibility = losslessEditors.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void UpdateLosslessScalingStatus()
        {
            try
            {
                bool isInstalled = losslessScalingInstalled?.Value ?? false;
                bool isRunning = losslessScalingRunning?.Value ?? false;

                Logger.Info($"UpdateLosslessScalingStatus called. Installed: {isInstalled}, Running: {isRunning}");

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        UpdateQuickSettingsTileStates();
                    }
                    catch (Exception innerEx)
                    {
                        Logger.Error($"Error updating Lossless Scaling tile state: {innerEx.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in UpdateLosslessScalingStatus: {ex.Message}");
            }
        }

        private void LosslessScalingStatus_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update status when installed/running state changes
            UpdateLosslessScalingStatus();
        }

        /// <summary>
        /// Shows or hides the Lossless Scaling Quick Settings tile based on whether Lossless
        /// Scaling is installed.
        /// </summary>
        private void SetLosslessScalingTileVisibility(bool installed)
        {
            if (quickSettingsInitialized)
            {
                RebuildQuickSettingsTiles();
                BuildSortableGrid();
                Logger.Info($"Rebuilt Quick Settings tiles for Lossless Scaling visibility: {installed}");
            }
        }
    }
}
