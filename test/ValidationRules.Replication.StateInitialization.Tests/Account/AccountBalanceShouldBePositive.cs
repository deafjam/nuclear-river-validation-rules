﻿using System.Collections.Generic;

using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.AccountRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement AccountBalanceShouldBePositive
            => ArrangeMetadataElement
                .Config
                .Name(nameof(AccountBalanceShouldBePositive))
                .Fact(
                      // Новый заказ, до этого не выходил ни разу на счете достаточно средств: ошибки нет
                      new Facts::Account { Id = 1, Balance = 11, BranchOfficeOrganizationUnitId = 1, LegalPersonId = 1 },

                      new Facts::Order { Id = 1, AgileDistributionStartDate = FirstDayFeb, AgileDistributionEndFactDate = FirstDayApr },
                      new Facts::OrderConsistency { Id = 1, BranchOfficeOrganizationUnitId = 1, LegalPersonId = 1 },
                      new Facts::OrderWorkflow { Id = 1, Step = 5 },
                      new Facts::OrderPosition { Id = 1, OrderId = 1 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 1, Amount = 10, Start = FirstDayFeb, End = FirstDayMar },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 1, Amount = 10, Start = FirstDayMar, End = FirstDayApr },

                      // Новый заказ, до этого не выходил ни разу с бесплатным типом: ошибки нет
                      new Facts::Account { Id = 2, Balance = 0, BranchOfficeOrganizationUnitId = 2, LegalPersonId = 2 },
                      new Facts::Order { Id = 2, AgileDistributionStartDate = FirstDayFeb, AgileDistributionEndFactDate = FirstDayMay },
                      new Facts::OrderConsistency { Id = 2, BranchOfficeOrganizationUnitId = 2, LegalPersonId = 2, IsFreeOfCharge = true },
                      new Facts::OrderWorkflow { Id = 2, Step = 5 },
                      new Facts::OrderPosition { Id = 2, OrderId = 2 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 2, Amount = 1, Start = FirstDayFeb, End = FirstDayMar },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 2, Amount = 2, Start = FirstDayMar, End = FirstDayApr },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 2, Amount = 2, Start = FirstDayApr, End = FirstDayMay },

                      // Новый заказ, до этого не выходил ни разу со скидкой 100 %: ошибки нет
                      new Facts::Account { Id = 3, Balance = 0, BranchOfficeOrganizationUnitId = 3, LegalPersonId = 3 },
                      new Facts::Order { Id = 3, AgileDistributionStartDate = FirstDayFeb, AgileDistributionEndFactDate = FirstDayMar },
                      new Facts::OrderConsistency { Id = 3, BranchOfficeOrganizationUnitId = 3, LegalPersonId = 3 },
                      new Facts::OrderWorkflow { Id = 3, Step = 5 },
                      new Facts::OrderPosition { Id = 3, OrderId = 3 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 3, Amount = 0, Start = FirstDayFeb, End = FirstDayMar },

                      // Новый заказ, до этого не выходил ни разу, у него есть UnlimitedOrder за это период: ошибки нет
                      new Facts::Account { Id = 4, Balance = -500, BranchOfficeOrganizationUnitId = 4, LegalPersonId = 4 },
                      new Facts::Order { Id = 4, AgileDistributionStartDate = FirstDayFeb, AgileDistributionEndFactDate = FirstDayMar },
                      new Facts::OrderConsistency { Id = 4, BranchOfficeOrganizationUnitId = 4, LegalPersonId = 4 },
                      new Facts::OrderWorkflow { Id = 4, Step = 5 },
                      new Facts::OrderPosition { Id = 4, OrderId = 4 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 4, Amount = 105000, Start = FirstDayFeb, End = FirstDayMar },
                      new Facts::UnlimitedOrder { OrderId = 4, PeriodStart = FirstDayFeb, PeriodEnd = FirstDayMar },

                      // Новый заказ, до этого не выходил ни разу: задолжность = RW за текущий месяц (баланс 0, задолжность 105000)
                      new Facts::Account { Id = 5, Balance = 0, BranchOfficeOrganizationUnitId = 5, LegalPersonId = 5 },
                      new Facts::Order { Id = 5, AgileDistributionStartDate = FirstDayFeb, AgileDistributionEndFactDate = FirstDayMar },
                      new Facts::OrderConsistency { Id = 5, BranchOfficeOrganizationUnitId = 5, LegalPersonId = 5 },
                      new Facts::OrderWorkflow { Id = 5, Step = 5 },
                      new Facts::OrderPosition { Id = 5, OrderId = 5 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 5, Amount = 105000, Start = FirstDayFeb, End = FirstDayMar },

                      // Заказ выходил в выпуск 1раз, у него есть списание за 1-й период: задолжность = RW за текущий месяц (баланс -1, задолжность 2)
                      new Facts::Account { Id = 6, Balance = -1, BranchOfficeOrganizationUnitId = 6, LegalPersonId = 6 },
                      new Facts::Order { Id = 6, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayMay },
                      new Facts::OrderConsistency { Id = 6, BranchOfficeOrganizationUnitId = 6, LegalPersonId = 6 },
                      new Facts::OrderWorkflow { Id = 6, Step = 5 },
                      new Facts::OrderPosition { Id = 6, OrderId = 6 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 6, Amount = 1, Start = FirstDayJan, End = FirstDayFeb },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 6, Amount = 2, Start = FirstDayFeb, End = FirstDayMar },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 6, Amount = 3, Start = FirstDayMar, End = FirstDayApr },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 6, Amount = 4, Start = FirstDayApr, End = FirstDayMay },
                      new Facts::AccountDetail { Id = 6, AccountId = 6, PeriodStartDate = FirstDayJan, OrderId = 6 },

                      // Заказ выходил в выпуск 1раз, у него нет списания за прошлый месяц: задолжность = RW за текущий месяц + RW за прошлый месяц (баланс 0, задолжность 3)
                      new Facts::Account { Id = 7, Balance = 0, BranchOfficeOrganizationUnitId = 7, LegalPersonId = 7 },
                      new Facts::Order { Id = 7, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayMar },
                      new Facts::OrderConsistency { Id = 7, BranchOfficeOrganizationUnitId = 7, LegalPersonId = 7 },
                      new Facts::OrderWorkflow { Id = 7, Step = 5 },
                      new Facts::OrderPosition { Id = 7, OrderId = 7 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 7, Amount = 1, Start = FirstDayJan, End = FirstDayFeb },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 7, Amount = 2, Start = FirstDayFeb, End = FirstDayMar },

                      // Заказ выходил в выпуск 1раз, скидка 100%, есть списание за 1-й период на 0 рублей: ошибки нет 
                      new Facts::Account { Id = 8, Balance = 0, BranchOfficeOrganizationUnitId = 8, LegalPersonId = 8 },
                      new Facts::Order { Id = 8, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayMar },
                      new Facts::OrderConsistency { Id = 8, BranchOfficeOrganizationUnitId = 8, LegalPersonId = 8 },
                      new Facts::OrderWorkflow { Id = 8, Step = 5 },
                      new Facts::OrderPosition { Id = 8, OrderId = 8 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 8, Amount = 0, Start = FirstDayJan, End = FirstDayFeb },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 8, Amount = 0, Start = FirstDayFeb, End = FirstDayMar },
                      new Facts::AccountDetail { Id = 8, AccountId = 8, PeriodStartDate = FirstDayJan, OrderId = 8 },

                      // Новый заказ, до этого не выходил ни разу со скидкой 100 %, баланс счета отрицательный: ошибки нет
                      new Facts::Account { Id = 9, Balance = -1, BranchOfficeOrganizationUnitId = 9, LegalPersonId = 9 },
                      new Facts::Order { Id = 9, AgileDistributionStartDate = FirstDayFeb, AgileDistributionEndFactDate = FirstDayMar },
                      new Facts::OrderConsistency { Id = 9, BranchOfficeOrganizationUnitId = 9, LegalPersonId = 9 },
                      new Facts::OrderWorkflow { Id = 9, Step = 5 },
                      new Facts::OrderPosition { Id = 9, OrderId = 9 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 9, Amount = 0, Start = FirstDayFeb, End = FirstDayMar },

                      // Заказ выходил в выпуск 1раз, в текущем месяце переведен в статус "4", у него нет списания за прошлый месяц: RW за текущий месяц + RW за прошлый месяц (баланс 0, задолжность -3)
                      new Facts::Account { Id = 10, Balance = 0, BranchOfficeOrganizationUnitId = 10, LegalPersonId = 10 },
                      new Facts::Order { Id = 10, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayApr },
                      new Facts::OrderConsistency { Id = 10, BranchOfficeOrganizationUnitId = 10, LegalPersonId = 10 },
                      new Facts::OrderWorkflow { Id = 10, Step = 4 },
                      new Facts::OrderPosition { Id = 10, OrderId = 10 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 10, Amount = 1, Start = FirstDayJan, End = FirstDayFeb },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 10, Amount = 2, Start = FirstDayFeb, End = FirstDayMar },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 10, Amount = 3, Start = FirstDayMar, End = FirstDayApr },

                      // Новый заказ, до этого не выходил ни разу с бесплатным типом, баланс счета отрицательный: ошибки нет
                      new Facts::Account { Id = 11, Balance = -1, BranchOfficeOrganizationUnitId = 11, LegalPersonId = 11 },
                      new Facts::Order { Id = 11, AgileDistributionStartDate = FirstDayFeb, AgileDistributionEndFactDate = FirstDayMar },
                      new Facts::OrderConsistency { Id = 11, BranchOfficeOrganizationUnitId = 11, LegalPersonId = 11, IsFreeOfCharge = true },
                      new Facts::OrderWorkflow { Id = 11, Step = 5 },
                      new Facts::OrderPosition { Id = 11, OrderId = 11 },
                      new Facts::ReleaseWithdrawal { OrderPositionId = 11, Amount = 3, Start = FirstDayFeb, End = FirstDayMar })

                .Aggregate(
                           new Account { Id = 1 },
                           new Account { Id = 2 },
                           new Account { Id = 3 },
                           new Account { Id = 4 },
                           new Account { Id = 5 },
                           new Account { Id = 6 },
                           new Account { Id = 7 },
                           new Account { Id = 8 },
                           new Account { Id = 9 },
                           new Account { Id = 10 },
                           new Account { Id = 11 },

                           new Order { Id = 1, AccountId = 1, Start = FirstDayFeb, End = FirstDayApr },
                           new Order { Id = 2, AccountId = 2, Start = FirstDayFeb, End = FirstDayMay, IsFreeOfCharge = true },
                           new Order { Id = 3, AccountId = 3, Start = FirstDayFeb, End = FirstDayMar },
                           new Order { Id = 4, AccountId = 4, Start = FirstDayFeb, End = FirstDayMar },
                           new Order { Id = 5, AccountId = 5, Start = FirstDayFeb, End = FirstDayMar },
                           new Order { Id = 6, AccountId = 6, Start = FirstDayJan, End = FirstDayMay },
                           new Order { Id = 7, AccountId = 7, Start = FirstDayJan, End = FirstDayMar },
                           new Order { Id = 8, AccountId = 8, Start = FirstDayJan, End = FirstDayMar },
                           new Order { Id = 9, AccountId = 9, Start = FirstDayFeb, End = FirstDayMar },
                           new Order { Id = 10, AccountId = 10, Start = FirstDayJan, End = FirstDayApr },
                           new Order { Id = 11, AccountId = 11, Start = FirstDayFeb, End = FirstDayMar, IsFreeOfCharge = true },

                           new Account.AccountPeriod { AccountId = 1, Balance = 1, ReleaseAmount = 10, Start = FirstDayMar, End = FirstDayApr },
                           new Account.AccountPeriod { AccountId = 4, Balance = -500, ReleaseAmount = 105000, Start = FirstDayFeb, End = FirstDayMar },
                           new Account.AccountPeriod { AccountId = 5, Balance = 0, ReleaseAmount = 105000, Start = FirstDayFeb, End = FirstDayMar },
                           new Account.AccountPeriod { AccountId = 6, Balance = -1, ReleaseAmount = 2, Start = FirstDayFeb, End = FirstDayMar },
                           new Account.AccountPeriod { AccountId = 6, Balance = -3, ReleaseAmount = 3, Start = FirstDayMar, End = FirstDayApr },
                           new Account.AccountPeriod { AccountId = 6, Balance = -6, ReleaseAmount = 4, Start = FirstDayApr, End = FirstDayMay },
                           new Account.AccountPeriod { AccountId = 7, Balance = 0, ReleaseAmount = 1, Start = FirstDayJan, End = FirstDayFeb },
                           new Account.AccountPeriod { AccountId = 7, Balance = -1, ReleaseAmount = 2, Start = FirstDayFeb, End = FirstDayMar },
                           new Account.AccountPeriod { AccountId = 10, Balance = 0, ReleaseAmount = 1, Start = FirstDayJan, End = FirstDayFeb },
                           new Account.AccountPeriod { AccountId = 10, Balance = -1, ReleaseAmount = 2, Start = FirstDayFeb, End = FirstDayMar },
                           new Account.AccountPeriod { AccountId = 10, Balance = -3, ReleaseAmount = 3, Start = FirstDayMar, End = FirstDayApr },

                           new Order.DebtPermission { OrderId = 4, Start = FirstDayFeb, End = FirstDayMar }
                )
                .Message(
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", 1.0000m }, { "planned", 10.0000m } },
                             new Reference<EntityTypeAccount>(1),
                             new Reference<EntityTypeOrder>(1)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayMar,
                             PeriodEnd = FirstDayApr,
                             OrderId = 1
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", 0.0000m }, { "planned", 105000.0000m } },
                             new Reference<EntityTypeAccount>(5),
                             new Reference<EntityTypeOrder>(5)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayFeb,
                             PeriodEnd = FirstDayMar,
                             OrderId = 5
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", -1.0000m }, { "planned", 2.0000m } },
                             new Reference<EntityTypeAccount>(6),
                             new Reference<EntityTypeOrder>(6)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayFeb,
                             PeriodEnd = FirstDayMar,
                             OrderId = 6
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", -3.0000m }, { "planned", 3.0000m } },
                             new Reference<EntityTypeAccount>(6),
                             new Reference<EntityTypeOrder>(6)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayMar,
                             PeriodEnd = FirstDayApr,
                             OrderId = 6
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", -6.0000m }, { "planned", 4.0000m } },
                             new Reference<EntityTypeAccount>(6),
                             new Reference<EntityTypeOrder>(6)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayApr,
                             PeriodEnd = FirstDayMay,
                             OrderId = 6
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", 0.0000m }, { "planned", 1.0000m } },
                             new Reference<EntityTypeAccount>(7),
                             new Reference<EntityTypeOrder>(7)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayJan,
                             PeriodEnd = FirstDayFeb,
                             OrderId = 7
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", -1.0000m }, { "planned", 2.0000m } },
                             new Reference<EntityTypeAccount>(7),
                             new Reference<EntityTypeOrder>(7)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayFeb,
                             PeriodEnd = FirstDayMar,
                             OrderId = 7
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", 0.0000m }, { "planned", 1.0000m } },
                             new Reference<EntityTypeAccount>(10),
                             new Reference<EntityTypeOrder>(10)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayJan,
                             PeriodEnd = FirstDayFeb,
                             OrderId = 10
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", -1.0000m }, { "planned", 2.0000m } },
                             new Reference<EntityTypeAccount>(10),
                             new Reference<EntityTypeOrder>(10)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayFeb,
                             PeriodEnd = FirstDayMar,
                             OrderId = 10
                         },
                         new Messages::Version.ValidationResult
                         {
                             MessageParams = new MessageParams(
                             new Dictionary<string, object> { { "available", -3.0000m }, { "planned", 3.0000m } },
                             new Reference<EntityTypeAccount>(10),
                             new Reference<EntityTypeOrder>(10)).ToXDocument(),
                             MessageType = (int)MessageTypeCode.AccountBalanceShouldBePositive,
                             PeriodStart = FirstDayMar,
                             PeriodEnd = FirstDayApr,
                             OrderId = 10
                         }
                );
    }
}