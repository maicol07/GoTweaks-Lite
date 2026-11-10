using Microsoft.Win32;
using Shared.Enums;
using XboxGamingBarHelper.ControllerEmulation.Viiper;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Settings
{
    internal sealed class UsbipInstalledProperty : HelperProperty<bool, SettingsManager>
    {
        public UsbipInstalledProperty(SettingsManager manager)
            : base(Detect(), null, Function.Viiper_UsbipInstalled, manager)
        {
        }

        private static bool Detect()
        {
            using (var driver = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\usbip2_ude"))
            {
                return driver != null && UsbipCli.ResolveExe() != null;
            }
        }
    }
}
