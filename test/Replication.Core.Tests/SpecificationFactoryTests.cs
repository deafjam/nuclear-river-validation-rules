using System.Linq;

using NuClear.Replication.Core.Specs;
using NuClear.Storage.API.Specifications;

using NUnit.Framework;

namespace NuClear.Replication.Core.Tests
{
    [TestFixture]
    public class SpecificationFactoryTests
    {
        private const int MsSqlBatchLimitation = 5000;

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(MsSqlBatchLimitation, 1)]
        [TestCase(MsSqlBatchLimitation + 1, 2)]
        public void TestBatches(int idCount, int batchCount)
        {
            var spec = SpecificationFactory<Foo>.Contains(foo => foo.Id, Enumerable.Repeat(0, idCount).ToArray());
            var batchSpecs = spec.WrappedSpecs;
            Assert.AreEqual(batchCount, batchSpecs.Count);
        }

        [Test]
        public void TestFilter()
        {
            var spec = SpecificationFactory<Foo>.Contains(foo => foo.Id, Enumerable.Range(0, MsSqlBatchLimitation).ToArray());
            var batchSpec = spec.WrappedSpecs.Single();
            var items = new[]
                {
                    new Foo { Id = -1 },
                    new Foo { Id = 0 },
                    new Foo { Id = MsSqlBatchLimitation - 1 },
                    new Foo { Id = MsSqlBatchLimitation },
                };

            var filteredItems = items.AsQueryable().Where(batchSpec);

            Assert.AreEqual(0, filteredItems.Count(x => x.Id == -1));
            Assert.AreEqual(1, filteredItems.Count(x => x.Id == 0));
            Assert.AreEqual(1, filteredItems.Count(x => x.Id == MsSqlBatchLimitation - 1));
            Assert.AreEqual(0, filteredItems.Count(x => x.Id == MsSqlBatchLimitation));
        }

        class Foo
        {
            public int Id { get; set; }
        }
    }
}
