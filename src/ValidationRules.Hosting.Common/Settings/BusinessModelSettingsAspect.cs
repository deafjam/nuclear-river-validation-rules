using NuClear.Settings;
using NuClear.Settings.API;

namespace NuClear.ValidationRules.Hosting.Common.Settings
{
    public sealed class BusinessModelSettingsAspect : ISettingsAspect, ISettings, IBusinessModelSettings
    {
        public string BusinessModel { get; } = ConfigFileSetting.String.Required("BusinessModel").Value;
    }
}