using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using NuClear.Replication.Core;
using NuClear.Replication.Core.Tenancy;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.ValidationRules.Hosting.Common.Identities.Connections;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed class RunnerConnectionStringSettings : ConnectionStringSettingsAspect, ITenantConnectionStringSettings
    {
        private static readonly IReadOnlyDictionary<IConnectionStringIdentity, string> ConnectionStrings =
            new Dictionary<IConnectionStringIdentity, string>
            {
                {
                    ValidationRulesConnectionStringIdentity.Instance,
                    ConfigurationManager.ConnectionStrings["ValidationRules"].ConnectionString
                },
                {
                    ErmConnectionStringIdentity.Instance,
                    ConfigurationManager.ConnectionStrings["Erm"].ConnectionString
                },
            };

        public RunnerConnectionStringSettings()
            : base(ConnectionStrings)
        {
        }

        public IReadOnlyDictionary<IConnectionStringIdentity, string> AllConnectionStrings =>
            ConnectionStrings;

        public string GetConnectionString(IConnectionStringIdentity identity, Tenant tenant)
        {
            throw new System.NotImplementedException();
        }
    }
}
