using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Specs;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.Facts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.ValidationRules.Replication.Accessors
{
    public sealed class AccountDetailAccessor : IStorageBasedDataObjectAccessor<AccountDetail>, IDataChangesHandler<AccountDetail>
    {
        private readonly IQuery _query;

        public AccountDetailAccessor(IQuery query) => _query = query;

        public IQueryable<AccountDetail> GetSource() =>
            // join тут можно использовать, т.к. AccountDetail это ValueObject для Account
            from account in _query.For(Specs.Find.Erm.Account)
            from accountDetail in _query.For<Storage.Model.Erm.AccountDetail>().Where(x => !x.IsDeleted).Where(x => x.OrderId != null).Where(x => x.AccountId == account.Id)
            select new AccountDetail
            {
                Id = accountDetail.Id,
                AccountId = accountDetail.AccountId,
                OrderId = accountDetail.OrderId.Value,
                PeriodStartDate = accountDetail.PeriodStartDate.Value,
            };

        public FindSpecification<AccountDetail> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<AccountDetail>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<AccountDetail> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<AccountDetail> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<AccountDetail> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<AccountDetail> dataObjects)
        {
            var accountIds = dataObjects.Select(x => x.AccountId).ToHashSet();
            
            return new[] {new RelatedDataObjectOutdatedEvent(typeof(AccountDetail), typeof(Account), accountIds)};
        }
    }
}