using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core.Tenancy;
using NuClear.ValidationRules.Querying.Host.Composition;
using NuClear.ValidationRules.SingleCheck.Tenancy;
using NuClear.ValidationRules.Storage.Model.Facts;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.DataAccess
{
    public class NameResolvingService
    {
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly ITenantProvider _tenantProvider;

        public NameResolvingService(IDataConnectionProvider connectionProvider, ITenantProvider tenantProvider)
        {
            _connectionProvider = connectionProvider;
            _tenantProvider = tenantProvider;
        }

        public ResolvedNameContainer Resolve(IReadOnlyCollection<Message> messages)
        {
            var references = messages
                .SelectMany(x => x.References)
                .Concat(messages.SelectMany(x => x.References).SelectMany(x => x.Children))
                .ToHashSet(Reference.Comparer);

            return new ResolvedNameContainer(Resolve(references));
        }

        private IReadOnlyDictionary<Reference, string> Resolve(IEnumerable<Reference> references)
        {
            var searchKeys = references.Select(x => new { x.Id, x.EntityType });

            // Есть сущности, общие для всех инсталляций - их имена имеют TenantId = 0;
            var tenants = new Tenant[] {0, _tenantProvider.Current};

            using (var connection = _connectionProvider.CreateConnection(DataConnectionName.ValidationRules))
            {
                return connection
                    .GetTable<EntityName>()
                    .Where(x => tenants.Contains(x.TenantId))
                    .Where(x => searchKeys.Contains(new { x.Id, x.EntityType }))
                    .ToDictionary(x => new Reference(x.EntityType, x.Id), x => x.Name);
            }
        }
    }
}
