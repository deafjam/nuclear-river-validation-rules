using System;
using System.Collections.Generic;
using System.Configuration;
using NuClear.Replication.Core.Tenancy;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.ValidationRules.Hosting.Common.Identities.Connections;

namespace NuClear.ValidationRules.StateInitialization.Host
{
    public class TenantConnectionStringSettings : ITenantConnectionStringSettings
    {
        public IReadOnlyDictionary<IConnectionStringIdentity, string> AllConnectionStrings
            => throw new NotSupportedException();

        public string GetConnectionString(IConnectionStringIdentity identity, Tenant tenant)
        {
            switch (identity)
            {
                case ErmConnectionStringIdentity _:
                    return ConfigurationManager.ConnectionStrings[$"Erm.{tenant:G}"]?.ConnectionString;

                default:
                    return GetConnectionString(identity);
            }
        }

        public string GetConnectionString(IConnectionStringIdentity identity)
        {
            switch (identity)
            {
                case ValidationRulesConnectionStringIdentity _:
                    return ConfigurationManager.ConnectionStrings["ValidationRules"].ConnectionString;

                case AmsConnectionStringIdentity _:
                    return ConfigurationManager.ConnectionStrings["Ams"].ConnectionString;

                case RulesetConnectionStringIdentity _:
                    return ConfigurationManager.ConnectionStrings["Rulesets"].ConnectionString;

                default:
                    throw new ArgumentException($"Unsupported identity type {identity.GetType().Name}", nameof(identity));
            }
        }
    }
}