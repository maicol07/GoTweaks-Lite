using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    /// <summary>
    /// Read-only mirror of the current refresh rate. It must not write back to the display:
    /// RefreshDisplaySettings re-reads it on every display change, and applying that read-back
    /// persisted transient values (e.g. 60Hz during boot mode switches).
    /// </summary>
    internal class RefreshRateProperty : HelperProperty<int, SystemManager>
    {
        public RefreshRateProperty(int inValue, SystemManager inManager) : base(inValue, null, Function.RefreshRate, inManager)
        {
        }
    }
}
