using System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace XboxGamingBar
{
    public sealed partial class GamingWidget
    {
        private async void UpdateViiperAvailability()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                bool supported = controllerEmulationAvailable.Value;
                bool available = supported && usbipInstalled.Value;
                ControllerEmulationEnabledToggle.IsEnabled = available || controllerEmulationEnabled.Value;
                ViiperSuppressPhysicalToggle.IsEnabled = available;
                ControllerEmulationStatusText.Text = !supported ? "Controller emulation is unavailable on this device."
                    : !available ? "usbip-win2 0.9.8.1 is required."
                    : viiperRuntimeStatus.Value;
            });
        }

        private async void UpdateViiperConfigVisibility()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string target = viiperDeviceType.Value;
                bool nintendo = target == "nintendo";
                ViiperSteamSubDevicePanel.Visibility = target == "steam-generic" || target == "steam-controller" ||
                    target == "steamdeck-generic" ? Visibility.Visible : Visibility.Collapsed;
                ViiperSonySubDevicePanel.Visibility = target == "sony" ? Visibility.Visible : Visibility.Collapsed;
                ViiperNintendoSubDevicePanel.Visibility = nintendo ? Visibility.Visible : Visibility.Collapsed;
                ViiperJoyconGyroPerHalfPanel.Visibility = nintendo && viiperNintendoSubDevice.Value == "joycon-pair"
                    ? Visibility.Visible : Visibility.Collapsed;
                UpdateViiperStickGyroSectionVisibility();
            });
        }
    }
}
