using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;
using Shared.Enums;
using Shared.IPC;
using Windows.Foundation.Collections;

namespace XboxGamingBarHelper.LosslessScaling
{
    internal partial class LosslessScalingManager
    {
        // Choice tokens come from the historical integration. The installed profile must also contain the field.
        private static readonly Dictionary<string, (string Label, string Choices, double Min, double Max)> ProfileFields =
            new Dictionary<string, (string, string, double, double)>
            {
                ["ScalingType"] = ("Upscaling", "Off|LS1|FSR|NIS|SGSR|BCAS|Anime4K|xBR|Sharp Bilinear|Integer|Nearest Neighbor", 0, 0),
                ["ScalingMode"] = ("Scaling mode", "Auto|Custom", 0, 0),
                ["ScaleFactor"] = ("Scale factor", null, 1, 5),
                ["FrameGeneration"] = ("Frame generation", "Off|LSFG2|LSFG3", 0, 0),
                ["LSFG3Mode1"] = ("LSFG 3 mode", "FIXED|ADAPTIVE", 0, 0),
                ["LSFG3Multiplier"] = ("LSFG 3 multiplier", null, 2, 20),
                ["LSFG3Target"] = ("LSFG 3 target FPS", null, 10, 500),
                ["LSFG2Mode"] = ("LSFG 2 multiplier", "X2|X3|X4", 0, 0),
                ["LSFGFlowScale"] = ("Flow scale", null, 25, 100),
                ["LSFGSize"] = ("LSFG size", "BALANCED|PERFORMANCE", 0, 0),
                ["SyncMode"] = ("Sync mode", "OFF|DEFAULT|VSYNC1|VSYNC2|VSYNC3|VSYNC4", 0, 0),
                ["CaptureApi"] = ("Capture API", "DXGI|WGC", 0, 0),
                ["QueueTarget"] = ("Queue target", "0|1|2", 0, 0),
                ["DrawFps"] = ("Show FPS", "false|true", 0, 0),
                ["HdrSupport"] = ("HDR support", "false|true", 0, 0),
                ["GsyncSupport"] = ("G-Sync support", "false|true", 0, 0),
                ["AutoScale"] = ("Auto scale", "false|true", 0, 0),
                ["AutoScaleDelay"] = ("Auto scale delay (ms)", null, 0, 60000),
            };

        public ValueSet HandleProfiles(PipeMessage request)
        {
            var response = new ValueSet
            {
                [nameof(PipeMessage.Command)] = (int)Command.Response,
                [nameof(PipeMessage.Function)] = (int)request.Function,
            };
            try
            {
                if (request.Command != Command.Get && request.Command != Command.Set)
                    throw new InvalidDataException("Unsupported Lossless Scaling request.");

                var file = new LosslessScalingProfileFile(SETTINGS_PATH);
                var profiles = file.ReadProfiles().ToArray();
                if (request.Command == Command.Set)
                {
                    using (var payload = JsonDocument.Parse(request.Content ?? string.Empty))
                    {
                        var root = payload.RootElement;
                        if (!root.TryGetProperty("revision", out var revision) || revision.GetString() != file.Revision)
                            throw new IOException("Settings changed. Reload the profiles before saving.");
                        if (!root.TryGetProperty("index", out var index) || !index.TryGetInt32(out int profileIndex) ||
                            profileIndex < 0 || profileIndex >= profiles.Length)
                            throw new InvalidDataException("Select a valid Lossless Scaling profile.");

                        var changes = new Dictionary<string, string>();
                        foreach (var change in root.GetProperty("fields").EnumerateObject())
                        {
                            string value = change.Value.GetString();
                            if (!ProfileFields.TryGetValue(change.Name, out var field) || profiles[profileIndex].Element(change.Name) == null ||
                                !IsValidProfileValue(change.Name, value, field))
                                throw new InvalidDataException($"Invalid value for {change.Name}.");
                            changes[change.Name] = value;
                        }

                        if (changes.Count != 0)
                        {
                            var processes = Process.GetProcessesByName(PROCESS_NAME);
                            foreach (var process in processes) process.Dispose();
                            if (processes.Length != 0)
                                throw new IOException("Exit Lossless Scaling from its tray icon before saving.");
                            file.Save(profileIndex, changes);
                            file = new LosslessScalingProfileFile(SETTINGS_PATH);
                            profiles = file.ReadProfiles().ToArray();
                        }
                    }
                }

                string exe = FindLosslessScalingExePath();
                response[nameof(PipeMessage.Content)] = JsonSerializer.Serialize(new
                {
                    revision = file.Revision,
                    version = File.Exists(exe) ? FileVersionInfo.GetVersionInfo(exe).ProductVersion : "",
                    profiles = profiles.Select((profile, index) => new
                    {
                        title = profile.Element("Title")?.Value ?? $"Profile {index + 1}",
                        fields = ProfileFields.Where(field => profile.Element(field.Key) != null).Select(field => new
                        {
                            key = field.Key,
                            label = field.Value.Label,
                            value = profile.Element(field.Key).Value,
                            choices = field.Value.Choices ?? string.Empty,
                        }),
                    }),
                });
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is XmlException ||
                                       ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException || ex is Win32Exception)
            {
                response["Error"] = ex is FileNotFoundException || ex is DirectoryNotFoundException
                    ? "Open Lossless Scaling once to create its settings." : ex.Message;
            }
            return response;
        }

        private static bool IsValidProfileValue(string key, string value, (string Label, string Choices, double Min, double Max) field)
        {
            if (field.Choices != null)
                return field.Choices.Split('|').Contains(value);
            return double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double number) &&
                   number >= field.Min && number <= field.Max && (key == "ScaleFactor" || key == "LSFG3Multiplier" || number == Math.Truncate(number));
        }
    }
}
