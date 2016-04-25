﻿using System;
using System.Linq;

using NuClear.CustomerIntelligence.Storage.Model.Bit;
using NuClear.CustomerIntelligence.Storage.Model.CI;
using NuClear.CustomerIntelligence.Storage.Model.Facts;
using NuClear.CustomerIntelligence.Storage.Model.Statistics;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;

using CategoryGroup = NuClear.CustomerIntelligence.Storage.Model.CI.CategoryGroup;
using Client = NuClear.CustomerIntelligence.Storage.Model.CI.Client;
using Firm = NuClear.CustomerIntelligence.Storage.Model.Facts.Firm;
using FirmForecast = NuClear.CustomerIntelligence.Storage.Model.Statistics.FirmForecast;
using Project = NuClear.CustomerIntelligence.Storage.Model.Facts.Project;
using ProjectCategoryStatistics = NuClear.CustomerIntelligence.Storage.Model.Statistics.ProjectCategoryStatistics;
using Territory = NuClear.CustomerIntelligence.Storage.Model.CI.Territory;

namespace NuClear.CustomerIntelligence.Replication.Specifications
{
    public static partial class Specs
    {
        public static partial class Map
        {
            public static partial class Facts
            {
                // ReSharper disable once InconsistentNaming
                public static class ToCI
                {
                    public static readonly MapSpecification<IQuery, IQueryable<CategoryGroup>> CategoryGroups =
                        new MapSpecification<IQuery, IQueryable<CategoryGroup>>(
                            q => from categoryGroup in q.For<Storage.Model.Facts.CategoryGroup>()
                                 select new CategoryGroup
                                        {
                                            Id = categoryGroup.Id,
                                            Name = categoryGroup.Name,
                                            Rate = categoryGroup.Rate
                                        });

                    public static readonly MapSpecification<IQuery, IQueryable<Client>> Clients =
                        new MapSpecification<IQuery, IQueryable<Client>>(
                            q =>
                            {
                                var clientRates = from firm in q.For<Firm>()
                                                  join firmAddress in q.For<FirmAddress>() on firm.Id equals firmAddress.FirmId
                                                  join categoryFirmAddress in q.For<CategoryFirmAddress>() on firmAddress.Id equals categoryFirmAddress.FirmAddressId
                                                  join categoryOrganizationUnit in q.For<CategoryOrganizationUnit>() on
                                                      new { categoryFirmAddress.CategoryId, firm.OrganizationUnitId }
                                                      equals new { categoryOrganizationUnit.CategoryId, categoryOrganizationUnit.OrganizationUnitId }
                                                  join categoryGroup in q.For<Storage.Model.Facts.CategoryGroup>() on categoryOrganizationUnit.CategoryGroupId equals categoryGroup.Id
                                                  group categoryGroup by firm.ClientId
                                                  into categoryGroups
                                                  select new { ClientId = categoryGroups.Key, CategoryGroupId = categoryGroups.Min(x => x.Id) };
                                return from client in q.For<Storage.Model.Facts.Client>()
                                       from rate in clientRates.Where(x => x.ClientId == client.Id).DefaultIfEmpty()
                                       select new Client
                                              {
                                                  Id = client.Id,
                                                  Name = client.Name,
                                                  CategoryGroupId = rate != null ? rate.CategoryGroupId : 0
                                              };
                            });

                    public static readonly MapSpecification<IQuery, IQueryable<ClientContact>> ClientContacts =
                        new MapSpecification<IQuery, IQueryable<ClientContact>>(
                            q => from contact in q.For<Contact>()
                                 select new ClientContact
                                        {
                                            ClientId = contact.ClientId,
                                            ContactId = contact.Id,
                                            Role = contact.Role,
                                        });

                    public static readonly MapSpecification<IQuery, IQueryable<Storage.Model.CI.Firm>> Firms =
                         new MapSpecification<IQuery, IQueryable<Storage.Model.CI.Firm>>(
                            q =>
                            {
                                // FIXME {all, 03.04.2015}: the obtained SQL is too complex and slow
                                var clientsHavingPhone = from contact in q.For<Contact>()
                                                         where contact.HasPhone
                                                         select (long?)contact.ClientId;
                                var clientsHavingWebsite = from contact in q.For<Contact>()
                                                           where contact.HasWebsite
                                                           select (long?)contact.ClientId;

                                var firmsHavingPhone = from firmContact in q.For<FirmContact>().Where(x => x.HasPhone)
                                                       join firmAddress in q.For<FirmAddress>() on firmContact.FirmAddressId equals firmAddress.Id
                                                       select firmAddress.FirmId;
                                var firmsHavingWebsite = from firmContact in q.For<FirmContact>().Where(x => x.HasWebsite)
                                                         join firmAddress in q.For<FirmAddress>() on firmContact.FirmAddressId equals firmAddress.Id
                                                         select firmAddress.FirmId;

                                // TODO {all, 02.04.2015}: CategoryGroupId processing
                                return from firm in q.For<Firm>()
                                       join project in q.For<Project>() on firm.OrganizationUnitId equals project.OrganizationUnitId
                                       join client in q.For<Storage.Model.Facts.Client>() on firm.ClientId equals client.Id into clients
                                       from client in clients.DefaultIfEmpty(new Storage.Model.Facts.Client())
                                       select new Storage.Model.CI.Firm
                                              {
                                                  Id = firm.Id,
                                                  Name = firm.Name,
                                                  CreatedOn = firm.CreatedOn,
                                                  LastDisqualifiedOn = firm.LastDisqualifiedOn ?? client.LastDisqualifiedOn,
                                                  LastDistributedOn = q.For<Order>()
                                                                       .Where(order => order.FirmId == firm.Id)
                                                                       .Select(order => order.EndDistributionDateFact)
                                                                       .Cast<DateTimeOffset?>()
                                                                       .Max(),
                                                  HasPhone = firmsHavingPhone.Contains(firm.Id) || client.HasPhone || clientsHavingPhone.Contains(firm.ClientId),
                                                  HasWebsite = firmsHavingWebsite.Contains(firm.Id) || client.HasWebsite || clientsHavingWebsite.Contains(firm.ClientId),
                                                  AddressCount = q.For<FirmAddress>().Count(fa => fa.FirmId == firm.Id),
                                                  CategoryGroupId = (from firmAddress in q.For<FirmAddress>()
                                                                     where firmAddress.FirmId == firm.Id
                                                                     join categoryFirmAddress in q.For<CategoryFirmAddress>() on firmAddress.Id equals categoryFirmAddress.FirmAddressId
                                                                     join categoryOrganizationUnit in q.For<CategoryOrganizationUnit>() on
                                                                         new { categoryFirmAddress.CategoryId, firm.OrganizationUnitId } equals
                                                                         new { categoryOrganizationUnit.CategoryId, categoryOrganizationUnit.OrganizationUnitId }
                                                                     join categoryGroup in q.For<Storage.Model.Facts.CategoryGroup>() on categoryOrganizationUnit.CategoryGroupId equals categoryGroup.Id
                                                                     orderby categoryGroup.Rate descending
                                                                     select categoryGroup.Id).FirstOrDefault(),
                                                  ClientId = firm.ClientId,
                                                  ProjectId = project.Id,
                                                  OwnerId = firm.OwnerId
                                              };
                            });

                    public static readonly MapSpecification<IQuery, IQueryable<FirmActivity>> FirmActivities =
                        new MapSpecification<IQuery, IQueryable<FirmActivity>>(
                            q =>
                            {
                                var firmActivities = q.For<Activity>()
                                                      .Where(x => x.FirmId.HasValue)
                                                      .GroupBy(x => x.FirmId)
                                                      .Select(group => new { FirmId = group.Key, LastActivityOn = group.Max(x => x.ModifiedOn) });
                                var clientActivities = q.For<Activity>()
                                                        .Where(x => x.ClientId.HasValue)
                                                        .GroupBy(x => x.ClientId)
                                                        .Select(group => new { ClientId = group.Key, LastActivityOn = group.Max(x => x.ModifiedOn) });

                                return from firm in q.For<Firm>()
                                       from lastFirmActivity in firmActivities.Where(x => x.FirmId == firm.Id).Select(x => (DateTimeOffset?)x.LastActivityOn).DefaultIfEmpty()
                                       from lastClientActivity in
                                           clientActivities.Where(x => x.ClientId == firm.ClientId).Select(x => (DateTimeOffset?)x.LastActivityOn).DefaultIfEmpty()
                                       select new FirmActivity
                                              {
                                                  FirmId = firm.Id,
                                                  LastActivityOn = lastFirmActivity != null && lastClientActivity != null
                                                                       ? (lastFirmActivity < lastClientActivity ? lastClientActivity : lastFirmActivity)
                                                                       : (lastClientActivity ?? lastFirmActivity),
                                              };
                            });

                    public static readonly MapSpecification<IQuery, IQueryable<FirmBalance>> FirmBalances =
                        new MapSpecification<IQuery, IQueryable<FirmBalance>>(
                            q => from firm in q.For<Firm>()
                                 join client in q.For<Storage.Model.Facts.Client>() on firm.ClientId equals client.Id
                                 join legalPerson in q.For<LegalPerson>() on client.Id equals legalPerson.ClientId
                                 join account in q.For<Account>() on legalPerson.Id equals account.LegalPersonId
                                 join branchOfficeOrganizationUnit in q.For<BranchOfficeOrganizationUnit>() on account.BranchOfficeOrganizationUnitId equals branchOfficeOrganizationUnit.Id
                                 join project in q.For<Project>() on branchOfficeOrganizationUnit.OrganizationUnitId equals project.OrganizationUnitId

                                 select new FirmBalance { ProjectId = project.Id, FirmId = firm.Id, AccountId = account.Id, Balance = account.Balance });

                    public static readonly MapSpecification<IQuery, IQueryable<FirmCategory1>> FirmCategories1 =
                        new MapSpecification<IQuery, IQueryable<FirmCategory1>>(
                            q => (from firmAddress in q.For<FirmAddress>()
                                  join categoryFirmAddress in q.For<CategoryFirmAddress>() on firmAddress.Id equals categoryFirmAddress.FirmAddressId
                                  join category3 in q.For<Category>().Where(x => x.Level == 3) on categoryFirmAddress.CategoryId equals category3.Id
                                  join category2 in q.For<Category>().Where(x => x.Level == 2) on category3.ParentId equals category2.Id
                                  join category1 in q.For<Category>().Where(x => x.Level == 1) on category2.ParentId equals category1.Id
                                  select new FirmCategory1
                                  {
                                      FirmId = firmAddress.FirmId,
                                      CategoryId = category1.Id
                                  }).Distinct());

                    public static readonly MapSpecification<IQuery, IQueryable<FirmCategory2>> FirmCategories2 =
                        new MapSpecification<IQuery, IQueryable<FirmCategory2>>(
                            q => (from firmAddress in q.For<FirmAddress>()
                                  join categoryFirmAddress in q.For<CategoryFirmAddress>() on firmAddress.Id equals categoryFirmAddress.FirmAddressId
                                  join category3 in q.For<Category>().Where(x => x.Level == 3) on categoryFirmAddress.CategoryId equals category3.Id
                                  join category2 in q.For<Category>().Where(x => x.Level == 2) on category3.ParentId equals category2.Id
                                  select new FirmCategory2
                                  {
                                      FirmId = firmAddress.FirmId,
                                      CategoryId = category2.Id
                                  }).Distinct());

                    public static readonly MapSpecification<IQuery, IQueryable<FirmTerritory>> FirmTerritories =
                        new MapSpecification<IQuery, IQueryable<FirmTerritory>>(
                            q => (from firmAddress in q.For<FirmAddress>()
                                  select new FirmTerritory { FirmId = firmAddress.FirmId, FirmAddressId = firmAddress.Id, TerritoryId = firmAddress.TerritoryId })
                                     .Distinct());

                    public static readonly MapSpecification<IQuery, IQueryable<Storage.Model.CI.Project>> Projects =
                        new MapSpecification<IQuery, IQueryable<Storage.Model.CI.Project>>(
                            q => from project in q.For<Project>()
                                 select new Storage.Model.CI.Project
                                        {
                                            Id = project.Id,
                                            Name = project.Name
                                        });

                    public static readonly MapSpecification<IQuery, IQueryable<ProjectCategory>> ProjectCategories =
                        new MapSpecification<IQuery, IQueryable<ProjectCategory>>(
                            q => from project in q.For<Project>()
                                 join categoryOrganizationUnit in q.For<CategoryOrganizationUnit>() on project.OrganizationUnitId equals categoryOrganizationUnit.OrganizationUnitId
                                 join category in q.For<Category>() on categoryOrganizationUnit.CategoryId equals category.Id
                                 from restriction in q.For<SalesModelCategoryRestriction>().Where(x => x.ProjectId == project.Id && x.CategoryId == category.Id).DefaultIfEmpty()
                                 select new ProjectCategory
                                 {
                                     ProjectId = project.Id,
                                     CategoryId = categoryOrganizationUnit.CategoryId,
                                     Name = category.Name,
                                     Level = category.Level,
                                     ParentId = category.ParentId,
                                     SalesModel = restriction == null ? 0 : restriction.SalesModel
                                 });

                    public static readonly MapSpecification<IQuery, IQueryable<Territory>> Territories =
                        new MapSpecification<IQuery, IQueryable<Territory>>(
                            q => from territory in q.For<Storage.Model.Facts.Territory>()
                                 join project in q.For<Project>() on territory.OrganizationUnitId equals project.OrganizationUnitId
                                 select new Territory
                                        {
                                            Id = territory.Id,
                                            Name = territory.Name,
                                            ProjectId = project.Id
                                        });

                    public static readonly MapSpecification<IQuery, IQueryable<ProjectStatistics>> ProjectStatistics =
                        new MapSpecification<IQuery, IQueryable<ProjectStatistics>>(
                            q => q.For<Project>().Select(x => new ProjectStatistics { Id = x.Id }));

                    public static readonly MapSpecification<IQuery, IQueryable<ProjectCategoryStatistics>> ProjectCategoryStatistics =
                        new MapSpecification<IQuery, IQueryable<ProjectCategoryStatistics>>(
                            q => from p in q.For<Project>()
                                 join c in q.For<CategoryOrganizationUnit>() on p.OrganizationUnitId equals c.OrganizationUnitId
                                 select new ProjectCategoryStatistics { ProjectId = p.Id, CategoryId = c.CategoryId });

                    public static readonly MapSpecification<IQuery, IQueryable<FirmCategory3>> FirmCategory3 =
                        new MapSpecification<IQuery, IQueryable<FirmCategory3>>(
                            q =>
                            {
                                var firmDtos = from firm in q.For<Firm>()
                                               join project in q.For<Project>() on firm.OrganizationUnitId equals project.OrganizationUnitId
                                               join firmAddress in q.For<FirmAddress>() on firm.Id equals firmAddress.FirmId
                                               join categoryFirmAddress in q.For<CategoryFirmAddress>() on firmAddress.Id equals categoryFirmAddress.FirmAddressId
                                               select new
                                               {
                                                   FirmId = firm.Id,
                                                   ProjectId = project.Id,
                                                   categoryFirmAddress.CategoryId
                                               };

                                var firmCounts = from firm in firmDtos
                                                 group firm by new { firm.ProjectId, firm.CategoryId }
                                                 into grp
                                                 select new
                                                 {
                                                     grp.Key.ProjectId,
                                                     grp.Key.CategoryId,
                                                     Count = grp.Count()
                                                 };

                                var categories3 = from firmDto in firmDtos.Distinct()
                                                  join firmCount in firmCounts on new { firmDto.ProjectId, firmDto.CategoryId } equals new { firmCount.ProjectId, firmCount.CategoryId }
                                                  join category in q.For<Category>() on firmDto.CategoryId equals category.Id
                                                  from firmStatistics in q.For<FirmCategoryStatistics>()
                                                                              .Where(x => x.FirmId == firmDto.FirmId && x.CategoryId == firmDto.CategoryId && x.ProjectId == firmDto.ProjectId)
                                                                              .DefaultIfEmpty()
                                                  from categoryStatistics in q.For<Storage.Model.Bit.ProjectCategoryStatistics>()
                                                                              .Where(x => x.CategoryId == firmDto.CategoryId && x.ProjectId == firmDto.ProjectId)
                                                                              .DefaultIfEmpty()
                                                  from forecast in q.For<FirmCategoryForecast>()
                                                                              .Where(x => x.CategoryId == firmDto.CategoryId && x.ProjectId == firmDto.ProjectId && x.FirmId == firmDto.FirmId)
                                                                              .DefaultIfEmpty()
                                                  select new FirmCategory3
                                                  {
                                                      ProjectId = firmDto.ProjectId,
                                                      CategoryId = firmDto.CategoryId,
                                                      FirmId = firmDto.FirmId,
                                                      Name = category.Name,
                                                      Hits = firmStatistics == null ? 0 : firmStatistics.Hits,
                                                      Shows = firmStatistics == null ? 0 : firmStatistics.Shows,
                                                      FirmCount = firmCount.Count,
                                                      AdvertisersShare = categoryStatistics == null ? 0 : Math.Min(1, (float)categoryStatistics.AdvertisersCount / firmCount.Count),
                                                      ForecastClick = forecast == null ? null : (int?)forecast.ForecastClick,
                                                      ForecastAmount = forecast == null ? null : (decimal?)forecast.ForecastAmount
                                                  };

                                return categories3;
                            });

                    public static readonly MapSpecification<IQuery, IQueryable<FirmForecast>> FirmForecast =
                        new MapSpecification<IQuery, IQueryable<FirmForecast>>(
                            q =>
                            {
                                var firmDtos = from firm in q.For<Firm>()
                                               join forecast in q.For<Storage.Model.Bit.FirmForecast>() on firm.Id equals forecast.FirmId
                                               select new FirmForecast
                                               {
                                                   ProjectId = forecast.ProjectId,
                                                   FirmId = firm.Id,
                                                   ForecastClick = forecast.ForecastClick,
                                                   ForecastAmount = forecast.ForecastAmount
                                               };

                                return firmDtos;
                            });
                }
            }
        }
    }
}