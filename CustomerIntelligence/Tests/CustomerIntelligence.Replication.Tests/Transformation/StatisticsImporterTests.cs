﻿using System;
using System.Linq;

using Moq;

using NuClear.AdvancedSearch.Common.Metadata.Elements;
using NuClear.CustomerIntelligence.Domain;
using NuClear.CustomerIntelligence.Domain.DTO;
using NuClear.Metamodeling.Elements;
using NuClear.Replication.Core.API;
using NuClear.Replication.Core.Facts;

using NUnit.Framework;

using Bit = NuClear.CustomerIntelligence.Domain.Model.Bit;

// ReSharper disable PossibleUnintendedReferenceComparison
namespace NuClear.CustomerIntelligence.Replication.Tests.Transformation
{
    [TestFixture]
    internal class StatisticsImporterTests
    {
        [Test]
        public void ShouldProduceCalculateStatisticsOperationForFirmStatisticsDto()
        {
            var dto = new FirmStatisticsDto
                      {
                          ProjectId = 1,
                          Firms = new[]
                                  {
                                      new FirmStatisticsDto.FirmDto
                                      {
                                            FirmId = 2,
                                            Categories = new[]
                                            {
                                                new FirmStatisticsDto.FirmDto.CategoryDto
                                                {
                                                    CategoryId = 3,
                                                    Hits = 4,
                                                    Shows = 5
                                                }
                                            }
                                      }
                                  }
                      };

            var query = new MemoryMockQuery(
                new Bit::FirmCategoryStatistics { ProjectId = 1, FirmId = 7 },
                new Bit::FirmCategoryStatistics { ProjectId = 2, FirmId = 8 });

            var metadataSource = new ImportStatisticsMetadataSource();
            IMetadataElement importStatisticsMetadata;
            if (!metadataSource.Metadata.Values.TryGetElementById(new Uri(typeof(FirmStatisticsDto).Name, UriKind.Relative), out importStatisticsMetadata))
            {
                throw new NotSupportedException(string.Format("The aggregate of type '{0}' is not supported.", typeof(FirmStatisticsDto)));
            }

            var importer = new StatisticsFactImporter<Bit::FirmCategoryStatistics, FirmStatisticsDto>(
                (ImportStatisticsMetadata<Bit::FirmCategoryStatistics, FirmStatisticsDto>)importStatisticsMetadata,
                query,
                Mock.Of<IBulkRepository<Bit::FirmCategoryStatistics>>());

            var operations = importer.Import(dto).ToArray();

            Assert.That(operations.Count(), Is.EqualTo(1));
            Assert.That(operations.Count(), Is.EqualTo(1));
        }

        [Test]
        public void ShouldProduceCalculateStatisticsOperationForCategoryStatisticsDto()
        {
            var dto = new CategoryStatisticsDto
            {
                ProjectId = 1,
                Categories = new[]
                                  {
                                      new CategoryStatisticsDto.CategoryDto
                                      {
                                          CategoryId = 2,
                                          AdvertisersCount = 3,
                                      }
                                  }
            };
            var query = new MemoryMockQuery(
                new Bit::ProjectCategoryStatistics { ProjectId = 1, CategoryId = 7 },
                new Bit::ProjectCategoryStatistics { ProjectId = 2, CategoryId = 7 });

            var metadataSource = new ImportStatisticsMetadataSource();
            IMetadataElement importStatisticsMetadata;
            if (!metadataSource.Metadata.Values.TryGetElementById(new Uri(typeof(CategoryStatisticsDto).Name, UriKind.Relative), out importStatisticsMetadata))
            {
                throw new NotSupportedException(string.Format("The aggregate of type '{0}' is not supported.", typeof(CategoryStatisticsDto)));
            }

            var importer = new StatisticsFactImporter<Bit::ProjectCategoryStatistics, CategoryStatisticsDto>(
                (ImportStatisticsMetadata<Bit::ProjectCategoryStatistics, CategoryStatisticsDto>)importStatisticsMetadata,
                query,
                Mock.Of<IBulkRepository<Bit::ProjectCategoryStatistics>>());

            var operations = importer.Import(dto).ToArray();

            Assert.That(operations.Count(), Is.EqualTo(1));
            Assert.That(operations.Count(), Is.EqualTo(1));
        }
    }
}