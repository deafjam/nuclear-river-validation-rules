using NuClear.Settings.API;

namespace NuClear.ValidationRules.Hosting.Common.Settings
{
    public interface IBusinessModelSettings : ISettings
    {
        string BusinessModel { get; }
    }
}
