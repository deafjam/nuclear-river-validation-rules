using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NuClear.Replication.Core.Tenancy;
using NuClear.ValidationRules.SingleCheck.Tenancy;

namespace NuClear.ValidationRules.Querying.Host.Tenancy
{
    public sealed class HttpContextTenantProvider : ITenantProvider
    {
        private const Tenant DefaultTenant = Tenant.Russia;
        private static readonly string[] SupportedSchemas = { "http://", "https://" };

        private static readonly IReadOnlyDictionary<string, Tenant> DomainTenantMap = new Dictionary<string, Tenant>
        {
            ["2gis.ae"] = Tenant.Emirates,
            ["2gis.az"] = Tenant.Azerbaijan,
            ["2gis.com.cy"] = Tenant.Cyprus,
            ["2gis.cz"] = Tenant.Czech,
            ["2gis.kg"] = Tenant.Kyrgyzstan,
            ["2gis.kz"] = Tenant.Kazakhstan,
            ["2gis.ru"] = Tenant.Russia,
            ["2gis.ua"] = Tenant.Ukraine,
            ["2gis.uz"] = Tenant.Uzbekistan,
        };

        public Tenant Current
        {
            get
            {
                var host = HttpContext.Current.Request.Headers.GetValues("Host")?.FirstOrDefault();

                if (string.IsNullOrEmpty(host))
                {
                    return DefaultTenant;
                }

                if (!SupportedSchemas.Any(schema => host.StartsWith(schema, StringComparison.InvariantCultureIgnoreCase)))
                {
                    host = $"http://{host}";
                }

                if (!Uri.TryCreate(host, UriKind.Absolute, out var originHostUrl))
                {
                    return DefaultTenant;
                }

                var originHost = originHostUrl.Host;

                var domainKey = DomainTenantMap.Keys.FirstOrDefault(x => originHost.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));

                return domainKey != null ? DomainTenantMap[domainKey] : DefaultTenant;
            }
        }
    }
}
