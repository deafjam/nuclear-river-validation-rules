﻿using NuClear.AdvancedSearch.Common.Metadata.Model;

namespace NuClear.CustomerIntelligence.Domain.Model.CI
{
    public sealed class CategoryGroup : ICustomerIntelligenceAggregatePart, IAggregateRoot
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public float Rate { get; set; }
    }
}