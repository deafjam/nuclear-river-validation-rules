﻿using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Storage.Model.Aggregates.AccountRules;
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

            public AccountAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator();

            public IQueryable<Account> GetSource()
                => _query.For<Facts::Account>().Select(x => new Account { Id = x.Id });

            public FindSpecification<Account> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Account>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class AccountPeriodAccessor : DataChangesHandler<Account.AccountPeriod>, IStorageBasedDataObjectAccessor<Account.AccountPeriod>
        {
            // todo: завести настройку SignificantDigitsNumber и вообще решить вопрос с настройками проверок
            private static readonly decimal Epsilon = 0.01m;

            private readonly IQuery _query;

            public AccountPeriodAccessor(IQuery query) : base(CreateInvalidator(x => GetRelatedOrders(query, x))) => _query = query;

            private static IRuleInvalidator CreateInvalidator(Func<IReadOnlyCollection<Account.AccountPeriod>, IEnumerable<long>> func)
                => new RuleInvalidator
                    {
                        {MessageTypeCode.AccountBalanceShouldBePositive, func}
                    };

            private static IEnumerable<long> GetRelatedOrders(IQuery query, IReadOnlyCollection<Account.AccountPeriod> dataObjects)
            {
                var accountIds = dataObjects.Select(x => x.AccountId).ToHashSet();
                return query.For<Order>().Where(x => accountIds.Contains(x.AccountId.Value)).Select(x => x.Id).Distinct();
            }

            public IQueryable<Account.AccountPeriod> GetSource()
            {
                var releaseWithdrawalPeriods =
                    from order in _query.For<Facts::Order>()
                    from orderConsistency in _query.For<Facts::OrderConsistency>().Where(x => !x.IsFreeOfCharge).Where(x => x.Id == order.Id)
                    from orderWorkflow in _query.For<Facts::OrderWorkflow>().Where(x => Facts::OrderWorkflowStep.Payable.Contains(x.Step)).Where(x => x.Id == order.Id)
                    from account in _query.For<Facts::Account>()
                                          .Where(x => x.LegalPersonId == orderConsistency.LegalPersonId && x.BranchOfficeOrganizationUnitId == orderConsistency.BranchOfficeOrganizationUnitId)
                    from orderPosition in _query.For<Facts::OrderPosition>().Where(x => x.OrderId == order.Id)
                    from releaseWithdrawal in _query.For<Facts::ReleaseWithdrawal>()
                                                    .Where(x => x.OrderPositionId == orderPosition.Id)
                                                    .Where(x => x.Start < order.AgileDistributionEndFactDate)
                    // фильтруем фактические списания, по ним ошибок быть уже не может
                    where !_query.For<Facts::AccountDetail>().Any(x => x.OrderId == order.Id && x.PeriodStartDate == releaseWithdrawal.Start)
                    select new
                    {
                        AccountId = account.Id,
                        releaseWithdrawal.Start,
                        releaseWithdrawal.End,
                        releaseWithdrawal.Amount,
                    };

                // накапливаем суммы по периодам: если 3 периода по 100$, то накопленная сумма будет 100$, 200$, 300$
                var result =
                    from period in releaseWithdrawalPeriods
                    group period by new { period.AccountId, period.Start, period.End }
                    into @group
                    let periodAmount = @group.Sum(x => x.Amount)
                    let lockedAmount = releaseWithdrawalPeriods.Where(x => x.AccountId == @group.Key.AccountId && x.Start <= @group.Key.Start).Sum(x => x.Amount)
                    from account in _query.For<Facts::Account>().Where(x => x.Id == @group.Key.AccountId)
                    where periodAmount > 0 && account.Balance + Epsilon <= lockedAmount
                    select new Account.AccountPeriod
                    {
                        AccountId = @group.Key.AccountId,
                        Balance = account.Balance - lockedAmount + periodAmount,
                        ReleaseAmount = periodAmount,
                        Start = @group.Key.Start,
                        End = @group.Key.End,
                    };

                return result;
            }

            public FindSpecification<Account.AccountPeriod> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Account.AccountPeriod>(x => aggregateIds.Contains(x.AccountId));
            }
        }
    }
}