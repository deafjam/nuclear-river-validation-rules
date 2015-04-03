﻿using System;
using System.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Model;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Data.Context.Implementation
{
    public sealed class CustomerIntelligenceTransformationContext : ICustomerIntelligenceContext
    {
        private readonly IFactsContext _context;

        public CustomerIntelligenceTransformationContext(IFactsContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            _context = context;
        }

        public IQueryable<Client> Clients
        {
            get
            {
                // TODO {all, 02.04.2015}: CategoryGroupId processing
                return from client in _context.Clients
                       select new Client
                              {
                                  Id = client.Id,
                                  Name = client.Name,
                                  //CategoryGroupId = null
                              };
            }
        }

        public IQueryable<Contact> Contacts
        {
            get
            {
                return from contact in _context.Contacts
                       select new Contact
                              {
                                  Id = contact.Id,
                                  Role = contact.Role,
                                  IsFired = contact.IsFired,
                                  ClientId = contact.ClientId
                              };
            }
        }

        public IQueryable<Firm> Firms
        {
            get
            {
                // FIXME {all, 03.04.2015}: the obtained SQL is too complex and slow

                var clientsHavingPhone = from contact in _context.Contacts
                                         where contact.HasPhone
                                         select contact.ClientId;
                var clientsHavingWebsite = from contact in _context.Contacts
                                           where contact.HasWebsite
                                           select contact.ClientId;

                var firmsHavingPhone = from firmContact in _context.FirmContacts.Where(x => x.HasPhone)
                                       join firmAddress in _context.FirmAddresses on firmContact.FirmAddressId equals firmAddress.Id
                                       select firmAddress.FirmId;
                var firmsHavingWebsite = from firmContact in _context.FirmContacts.Where(x => x.HasWebsite)
                                         join firmAddress in _context.FirmAddresses on firmContact.FirmAddressId equals firmAddress.Id
                                         select firmAddress.FirmId;

                // TODO {all, 02.04.2015}: CategoryGroupId processing
                return from firm in _context.Firms
                       join client in _context.Clients on firm.ClientId equals client.Id into firmClients
                       from firmClient in firmClients.DefaultIfEmpty()
                       select new Firm
                              {
                                  Id = firm.Id,
                                  Name = firm.Name,
                                  CreatedOn = firm.CreatedOn,
                                  LastDisqualifiedOn = (firmClient != null ? firmClient.LastDisqualifiedOn : firm.LastDisqualifiedOn),
                                  LastDistributedOn = _context.Orders.Where(o => o.FirmId == firm.Id).Select(d => d.EndDistributionDateFact).Cast<DateTimeOffset?>().Max(),
                                  HasPhone = firmsHavingPhone.Contains(firm.Id) || (firmClient != null && firmClient.HasPhone) || (firm.ClientId != null && clientsHavingPhone.Contains(firm.ClientId.Value)),
                                  HasWebsite = firmsHavingWebsite.Contains(firm.Id) || (firmClient != null && firmClient.HasWebsite) || (firm.ClientId != null && clientsHavingWebsite.Contains(firm.ClientId.Value)),
                                  AddressCount = _context.FirmAddresses.Count(fa => fa.FirmId == firm.Id),
                                  //CategoryGroupId = null,
                                  ClientId = firm.ClientId,
                                  OrganizationUnitId = firm.OrganizationUnitId,
                                  TerritoryId = firm.TerritoryId
                              };
            }
        }

        public IQueryable<FirmBalance> FirmBalances
        {
            get
            {
                return from firm in _context.Firms
                       join legalPerson in _context.LegalPersons on firm.ClientId equals legalPerson.ClientId
                       join account in _context.Accounts on legalPerson.Id equals account.LegalPersonId
                       select new FirmBalance
                              {
                                  AccountId = account.Id,
                                  FirmId = firm.Id,
                                  Balance = account.Balance
                              };
            }
        }

        public IQueryable<FirmCategory> FirmCategories
        {
            get
            {
                // TODO {all, 02.04.2015}: it's needed to resolve links up to level1 and level2
                return (from categoryFirmAddress in _context.CategoryFirmAddresses
                        join firmAddress in _context.FirmAddresses on categoryFirmAddress.FirmAddressId equals firmAddress.Id
                        select new FirmCategory
                               {
                                   FirmId = firmAddress.FirmId,
                                   CategoryId = categoryFirmAddress.CategoryId
                               }).Distinct();
            }
        }

        public IQueryable<FirmCategoryGroup> FirmCategoryGroups
        {
            get
            {
                return (from firm in _context.Firms
                        join firmAddress in _context.FirmAddresses on firm.Id equals firmAddress.FirmId
                        join categoryFirmAddress in _context.CategoryFirmAddresses on firmAddress.Id equals categoryFirmAddress.FirmAddressId
                        join categoryOrganizationUnit in _context.CategoryOrganizationUnits on categoryFirmAddress.CategoryId equals categoryOrganizationUnit.CategoryId
                        where firm.OrganizationUnitId == categoryOrganizationUnit.OrganizationUnitId
                        select new FirmCategoryGroup
                               {
                                   FirmId = firmAddress.FirmId,
                                   CategoryGroupId = categoryOrganizationUnit.CategoryGroupId
                               }).Distinct();
            }
        }
    }
}