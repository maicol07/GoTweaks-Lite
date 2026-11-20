using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    /// <summary>
    /// Read-only mirror of the current resolution. It must not write back to the display:
    /// re-applying a read-back resolution without DM_DISPLAYFREQUENCY lets the driver pick 60Hz.
    /// </summary>
    internal class ResolutionProperty : HelperProperty<string, SystemManager>
    {
        public ResolutionProperty(string inValue, SystemManager inManager) : base(inValue, null, Function.Resolution, inManager)
        {
        }
    }
}
