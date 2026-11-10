using System.Xml.Linq;
using XboxGamingBarHelper.LosslessScaling;
using Xunit;

namespace Shared.Tests;

public sealed class LosslessScalingProfileFileTests : IDisposable
{
    private const string Settings = """
        <Settings>
          <Hotkey>S</Hotkey>
          <GameProfiles>
            <Profile><Title>Localized default</Title><ScaleFactor>1</ScaleFactor><QueueTarget>2</QueueTarget></Profile>
            <Profile><Title>Example</Title><ScaleFactor>2</ScaleFactor></Profile>
          </GameProfiles>
        </Settings>
        """;

    private readonly string directory = Directory.CreateTempSubdirectory().FullName;
    private string SettingsPath => Path.Combine(directory, "Settings.xml");

    [Fact]
    public void SavePreservesOtherFieldsAndProfilesAndBacksUpOriginal()
    {
        File.WriteAllText(SettingsPath, Settings);
        new LosslessScalingProfileFile(SettingsPath).Save(0, new Dictionary<string, string> { ["ScaleFactor"] = "1.5" });

        Assert.Equal(Settings, File.ReadAllText(SettingsPath + ".gotweaks.bak"));
        var expected = XDocument.Parse(Settings.Replace("<ScaleFactor>1</ScaleFactor>", "<ScaleFactor>1.5</ScaleFactor>"));
        Assert.True(XNode.DeepEquals(expected, XDocument.Load(SettingsPath)));
    }

    [Fact]
    public void SaveRejectsChangesMadeAfterReading()
    {
        File.WriteAllText(SettingsPath, Settings);
        var snapshot = new LosslessScalingProfileFile(SettingsPath);
        string updated = Settings.Replace("<Hotkey>S</Hotkey>", "<Hotkey>F</Hotkey>");
        File.WriteAllText(SettingsPath, updated);

        Assert.Throws<IOException>(() => snapshot.Save(0, new Dictionary<string, string> { ["ScaleFactor"] = "1.5" }));
        Assert.Equal(updated, File.ReadAllText(SettingsPath));
        Assert.Single(Directory.GetFiles(directory));
    }

    [Theory]
    [InlineData("Title")]
    [InlineData("FSROptimize")]
    public void SaveRejectsProfileIdentityAndFieldsMissingFromInstalledSchema(string field)
    {
        File.WriteAllText(SettingsPath, Settings);
        var snapshot = new LosslessScalingProfileFile(SettingsPath);

        Assert.Throws<InvalidDataException>(() => snapshot.Save(0, new Dictionary<string, string> { [field] = "true" }));
        Assert.Equal(Settings, File.ReadAllText(SettingsPath));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}
