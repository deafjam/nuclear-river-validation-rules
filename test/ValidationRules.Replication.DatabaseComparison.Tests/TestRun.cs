﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using NUnit.Framework;

namespace ValidationRules.Replication.DatabaseComparison.Tests
{
    [TestFixture]
    public sealed class TestRun
    {
        private static readonly Type[] ExcludedTypes =
        {
            // causes OutOfMemoryException
            typeof(NuClear.ValidationRules.Storage.Model.Facts.EntityName)
        };

        public static IEnumerable TestCaseData()
        {
            return TestCaseDataFor(StorageDescriptor.Erm, StorageDescriptor.Facts)
                    .Concat(TestCaseDataFor(StorageDescriptor.Facts, StorageDescriptor.Aggregates))
                    .Concat(TestCaseDataFor(StorageDescriptor.Aggregates, StorageDescriptor.Messages));
        }

        // TODO: параллельный запуск
        [TestCaseSource(nameof(TestCaseData))]
        public void DatabaseComparison(Type dataObjectType, StorageDescriptor sourceDescriptor, StorageDescriptor destDescriptor)
        {
            var detector = ChangeDetector.Create(dataObjectType);
            var changes = detector.ProcessTwice(sourceDescriptor, destDescriptor);

            Assert.That(AsJson(changes.SourceOnly), Is.Empty, "Extra items in Source");
            Assert.That(AsJson(changes.DestOnly), Is.Empty, "Extra items in Dest");
            Assert.That(AsJson(changes.SourceChanged), Is.EquivalentTo(AsJson(changes.DestChanged)), "Items changed in Source and Dest");
        }

        private static IEnumerable<TestCaseData> TestCaseDataFor(StorageDescriptor sourceDescriptor, StorageDescriptor destDescriptor)
            => TypeProvider.GetDataObjectTypes(destDescriptor.MappingSchema)
               .Where(x => !ExcludedTypes.Contains(x))
               .Select(x => new TestCaseData(x, sourceDescriptor, destDescriptor)
                            .SetName(TestName(x)));

        public IEnumerable<object> AsJson(IEnumerable<object> enumerable)
            => enumerable.Select(x => JsonConvert.SerializeObject(x, Formatting.None));

        private static string TestName(Type type)
        {
            const string SearchPattern = "Model.";

            var fullName = type.FullName;
            if (fullName == null)
            {
                throw new ArgumentException("Type fullname is null");
            }

            var index = fullName.IndexOf(SearchPattern, StringComparison.OrdinalIgnoreCase);
            return index != -1 ? fullName.Substring(index + SearchPattern.Length) : fullName;
        }
    }
}