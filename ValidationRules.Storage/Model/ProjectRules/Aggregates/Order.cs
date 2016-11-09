﻿using System;

namespace NuClear.ValidationRules.Storage.Model.ProjectRules.Aggregates
{
    public sealed class Order
    {
        public long Id { get; set; }
        public long ProjectId { get; set; }
        public string Number { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public bool IsDraft { get; set; }

        public sealed class AddressAdvertisement
        {
            public long OrderId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
            public long AddressId { get; set; }
            public bool MustBeLocatedOnTheMap { get; set; } // ?
        }

        public class CategoryAdvertisement
        {
            public long OrderId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
            public long CategoryId { get; set; }
            public int SalesModel { get; set; }
            public bool IsSalesModelRestrictionApplicable { get; set; }
        }

        public class CostPerClickAdvertisement
        {
            public long OrderId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
            public long CategoryId { get; set; }
            public decimal Bid { get; set; }
        }
    }
}