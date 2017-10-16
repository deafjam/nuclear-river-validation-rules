﻿using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Storage.Model.AccountRules.Aggregates;
using NuClear.ValidationRules.Storage.Model.Messages;

using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.AccountRules.Aggregates
{
    public sealed class AccountAggregateRootActor : AggregateRootActor<Account>
    {
        public AccountAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Account> accountBulkRepository,
            IBulkRepository<Account.AccountPeriod> accountPeriodBulkRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new AccountAccessor(query), accountBulkRepository,
                HasValueObject(new AccountPeriodAccessor(query), accountPeriodBulkRepository));
        }

        public sealed class AccountAccessor : DataChangesHandler<Account>, IStorageBasedDataObjectAccessor<Account>
        {
            private readonly IQuery _query;

            public AccountAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator();

            public IQueryable<Account> GetSource()
                => _query.For<Facts::Account>().Select(x => new Account { Id = x.Id });

            public FindSpecification<Account> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<CreateDataObjectCommand>().Select(c => c.DataObjectId)
                                           .Concat(commands.OfType<SyncDataObjectCommand>().Select(c => c.DataObjectId))
                                           .Concat(commands.OfType<DeleteDataObjectCommand>().Select(c => c.DataObjectId))
                                           .Distinct()
                                           .ToArray();
                return new FindSpecification<Account>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class AccountPeriodAccessor : DataChangesHandler<Account.AccountPeriod>, IStorageBasedDataObjectAccessor<Account.AccountPeriod>
        {
            private readonly IQuery _query;

            public AccountPeriodAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.AccountBalanceShouldBePositive
                    };

            public IQueryable<Account.AccountPeriod> GetSource()
            {
                // баланса ЛС должно хватить на 2 планируемых списания - за текущий и следующий месяцы
                var releaseWithdrawals =
                    from first in _query.For<Facts::ReleaseWithdrawal>()
                    from second in _query.For<Facts::ReleaseWithdrawal>().Where(x => x.OrderPositionId == first.OrderPositionId && x.Start == first.End)
                    select new
                    {
                        second.OrderPositionId,
                        second.Start,
                        second.End,
                        Amount = first.Amount + second.Amount
                    };

                var releaseWithdrawalPeriods =
                    from order in _query.For<Facts::Order>().Where(x => !x.IsFreeOfCharge && Facts::Order.State.Payable.Contains(x.WorkflowStep))
                    from account in _query.For<Facts::Account>()
                                          .Where(x => x.LegalPersonId == order.LegalPersonId && x.BranchOfficeOrganizationUnitId == order.BranchOfficeOrganizationUnitId)
                    from orderPosition in _query.For<Facts::OrderPosition>().Where(x => x.OrderId == order.Id)
                    from releaseWithdrawal in releaseWithdrawals
                                                    .Where(x => x.OrderPositionId == orderPosition.Id)
                                                    .Where(x => x.Start < order.EndDistributionFact)
                    // фильтруем фактические списания, по ним ошибок быть уже не может
                    where !_query.For<Facts::AccountDetail>().Any(x => x.AccountId == account.Id && x.OrderId == order.Id && x.PeriodStartDate == releaseWithdrawal.Start)
                    select new
                    {
                        AccountId = account.Id,
                        releaseWithdrawal.Start,
                        releaseWithdrawal.End,
                        releaseWithdrawal.Amount,
                    };

                var result =
                    from item in releaseWithdrawalPeriods.GroupBy(x => new { x.AccountId, x.Start, x.End })
                    from account in _query.For<Facts::Account>().Where(x => x.Id == item.Key.AccountId)
                    select new Account.AccountPeriod
                    {
                        AccountId = item.Key.AccountId,
                        Start = item.Key.Start,
                        End = item.Key.End,
                        Balance = account.Balance,
                        ReleaseAmount = item.Select(x => x.Amount).Sum(),
                    };

                return result;
            }

            public FindSpecification<Account.AccountPeriod> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().Select(c => c.AggregateRootId).Distinct().ToArray();
                return new FindSpecification<Account.AccountPeriod>(x => aggregateIds.Contains(x.AccountId));
            }
        }
    }
}