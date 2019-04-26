using NuClear.Storage.API.ConnectionStrings;
using NuClear.ValidationRules.Storage.Connections;

using ValidationRules.Hosting.Common.Settings.Connections;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed class RunnerConnectionStringSettings : ConnectionStringSettingsAspect
    {
        public RunnerConnectionStringSettings()
            : base(ConnectionStrings.For(ErmConnectionStringIdentity.Instance,
                                         ValidationRulesConnectionStringIdentity.Instance))
        {
        }
    }
}