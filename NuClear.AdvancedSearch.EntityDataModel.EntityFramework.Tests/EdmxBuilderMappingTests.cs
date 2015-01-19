using System.ComponentModel;
using System.Reflection;

using NuClear.AdvancedSearch.EntityDataModel.Metadata;

using NUnit.Framework;

namespace EntityDataModel.EntityFramework.Tests
{
    [TestFixture]
    internal class EdmxBuilderMappingTests : EdmxBuilderBaseFixture
    {
        [Test]
        public void ShouldExposeEntitySets()
        {
            const string Book = "Book";
            const string Name = "Name";

            var config = NewContext("Library")
                .ConceptualModel(NewModel(NewEntity("Book")))
                .StoreModel(NewModel(NewEntity("Book")))
                .Mapping(
                    ModelMappingElement.Config.Mappings(
                        EntityMappingElement.Config.Map("Book", "Book")
                    )
                )
                ;

            var model = BuildModel(config);

            Assert.That(model, Is.Not.Null);
        }
    }

    public class MyType<T> : TypeDelegator
    {
        public MyType()
            : base(typeof(T))
        {
        }
    }

    public class Firm
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}