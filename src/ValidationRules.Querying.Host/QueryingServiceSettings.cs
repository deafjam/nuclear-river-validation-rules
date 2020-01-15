using NuClear.River.Hosting.Common.Identities.Connections;
using NuClear.River.Hosting.Common.Settings;
using NuClear.Settings.API;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.ValidationRules.Hosting.Common.Identities.Connections;
using NuClear.ValidationRules.Hosting.Common.Settings.Connections;

namespace NuClear.ValidationRules.Querying.Host
{
    internal sealed class QueryingServiceSettings : SettingsContainerBase
    {
        public QueryingServiceSettings()
        {
            var connectionString = ConnectionStrings.For(ErmConnectionStringIdentity.Instance,
                                                         KafkaConnectionStringIdentity.Instance,
                                                         ValidationRulesConnectionStringIdentity.Instance,
                                                         LoggingConnectionStringIdentity.Instance);

            Aspects.Use(new ConnectionStringSettingsAspect(connectionString))
                   .Use<EnvironmentSettingsAspect>();
        }
    }
}