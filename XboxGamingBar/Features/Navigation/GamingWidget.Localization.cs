using Windows.ApplicationModel.Resources;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        // XAML text is localized through x:Uid; this covers strings built in code.
        private static readonly ResourceLoader uiStrings = ResourceLoader.GetForViewIndependentUse();

        /// <summary>Strings/&lt;lang&gt;/Resources.resw value for <paramref name="key"/>, or the English fallback.</summary>
        internal static string LocalizedString(string key, string fallback)
        {
            string value = uiStrings.GetString(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static readonly string TileStateOn = LocalizedString("TileState_On", "On");
        private static readonly string TileStateOff = LocalizedString("TileState_Off", "Off");
        private static readonly string TileStateAction = LocalizedString("TileState_Action", "Action");
    }
}
