using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Settings
{
    internal sealed class ViiperSuppressPhysicalProperty : HelperProperty<bool, SettingsManager>
    {
        private const string Key = "ViiperSuppressPhysical";

        public ViiperSuppressPhysicalProperty(SettingsManager manager)
            : base(LocalSettingsHelper.TryGetValue<bool>(Key, out var enabled) && enabled,
                   null, Function.ViiperSuppressPhysical, manager)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            LocalSettingsHelper.SetValue(Key, Value);
            base.NotifyPropertyChanged(propertyName);
        }
    }
}
