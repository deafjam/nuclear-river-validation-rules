using System.Collections.Generic;
using System.Reflection;

using Moq;

using NuClear.Replication.Core.Equality;

using NUnit.Framework;

namespace NuClear.Replication.Core.Tests
{
    [TestFixture]
    public class EqualityComparerFactoryTests
    {
        class SampleEntity
        {
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Id;
                    hashCode = (hashCode * 397) ^ NullableId.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public int Id { get; set; }
            public int? NullableId { get; set; }
            public string Name { get; set; }
        }

        [TestCase(1, 1, "abc")]
        [TestCase(1, null, "abc")]
        [TestCase(1, 1, null)]
        public void GetHashCodeShouldBeEqaulToResharperGenerated(int id, int? nullable, string name)
        {
            var provider = new Mock<IObjectPropertyProvider>();
            var props = typeof(SampleEntity).GetProperties();
            provider.Setup(x => x.GetProperties<SampleEntity>()).Returns(props);
            provider.Setup(x => x.GetPrimaryKeyProperties<SampleEntity>()).Returns(new List<PropertyInfo>());

            var factory = new EqualityComparerFactory(provider.Object);
            var comparer = factory.CreateCompleteComparer<SampleEntity>();

            var left = new SampleEntity { Id = id, NullableId = nullable, Name = name };

            Assert.That(comparer.GetHashCode(left), Is.EqualTo(left.GetHashCode()));
        }

        [TestCase(1, -1, false)]
        [TestCase(-1, -1, true)]
        [TestCase(-1, -2, true)]
        [TestCase(1, 1, true)]
        public void EqualityShouldRespectProvidedComparers(int idLeft, int idRight, bool expected)
        {
            var provider = new Mock<IObjectPropertyProvider>();
            var props = typeof(SampleEntity).GetProperties();
            provider.Setup(x => x.GetProperties<SampleEntity>()).Returns(props);
            provider.Setup(x => x.GetPrimaryKeyProperties<SampleEntity>()).Returns(new List<PropertyInfo>());

            var factory = new EqualityComparerFactory(provider.Object, new CustomIntegerComparer());
            var comparer = factory.CreateCompleteComparer<SampleEntity>();

            var left = new SampleEntity { Id = idLeft };
            var right = new SampleEntity { Id = idRight };

            Assert.AreEqual(expected, comparer.GetHashCode(left) == comparer.GetHashCode(right));
            Assert.AreEqual(expected, comparer.Equals(left, right));
        }

        /// <summary>
        /// Считает все отрицательные числа равными.
        /// </summary>
        private class CustomIntegerComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
                => x < 0 && y < 0 || x == y;

            public int GetHashCode(int obj)
                => obj < 0 ? -1 : obj;
        }
    }
}
