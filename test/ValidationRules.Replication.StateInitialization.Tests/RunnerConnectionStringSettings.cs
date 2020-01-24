using System;
using System.Collections.Generic;
using NuClear.Replication.Core;
using NuClear.Replication.Core.Tenancy;
using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed class RunnerConnectionStringSettings : ITenantConnectionStringSettings
    {
        public IReadOnlyDictionary<IConnectionStringIdentity, string> AllConnectionStrings
            => throw new NotSupportedException();

        public string GetConnectionString(IConnectionStringIdentity connectionStringIdentity)
        {
            throw new System.NotImplementedException();
        }

        public string GetConnectionString(IConnectionStringIdentity identity, Tenant tenant)
        {
            throw new System.NotImplementedException();
        }
    }
}