using Microsoft.Practices.Unity;
using NuClear.Assembling.TypeProcessing;
using NuClear.DataTest.Engine;
using NuClear.DataTest.Engine.Command;
using NuClear.DataTest.Metamodel;
using NuClear.DataTest.Metamodel.Dsl;
using NuClear.Metamodeling.Processors;
using NuClear.Metamodeling.Provider;
using NuClear.Metamodeling.Provider.Sources;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.ValidationRules.Replication.StateInitialization.Tests.DI;
using NuClear.ValidationRules.Replication.StateInitialization.Tests.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ContextEntityTypesProvider = NuClear.ValidationRules.Replication.StateInitialization.Tests.Infrastructure.ContextEntityTypesProvider;
using CreateDatabaseSchemataCommand = NuClear.ValidationRules.Replication.StateInitialization.Tests.Infrastructure.CreateDatabaseSchemataCommand;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    [TestFixture]
    public sealed class DataTests
    {
        private UnityContainer _container;
        private MetadataProvider _metadataProvider;
        private TestRunner _testRunner;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            StateInitializationTestsRoot.Instance.PerformTypesMassProcessing(Array.Empty<IMassProcessor>(), true, typeof(object));
            _container = new UnityContainer();

            _metadataProvider =
                new MetadataProvider(
                    new IMetadataSource[] { MetadataSources.SchemaMetadataSource, MetadataSources.TestCaseMetadataSource },
                    Array.Empty<IMetadataProcessor>());

            _container.RegisterType<ConnectionStringSettingsAspect, RunnerConnectionStringSettings>()
                      .RegisterType<DataConnectionFactory>()
                      .RegisterInstance<IMetadataProvider>(_metadataProvider)
                      .RegisterType<IContextEntityTypesProvider, ContextEntityTypesProvider>();

            var dropDatabases = _container.Resolve<DropDatabasesCommand>();
            var createDatabases = _container.Resolve<CreateDatabasesCommand>();
            var createSchemata = _container.Resolve<CreateDatabaseSchemataCommand>();

            //dropDatabases.Execute();
            //createDatabases.Execute();
            //createSchemata.Execute();

            _testRunner = _container.Resolve<TestRunner>();
        }

        [SetUp]
        public void SetUp()
        {
        }

        [TestCaseSource(typeof(MetadataSources), nameof(MetadataSources.Tests))]
        public void Test(TestCaseMetadataElement testCase)
        {
            Assume.That(testCase != null);
            Assume.That(!testCase.Arrange.IsIgnored);

            _testRunner.Execute(testCase);
        }

        private static class MetadataSources
        {
            public static TestCaseMetadataSource TestCaseMetadataSource { get; }
            public static IReadOnlyCollection<TestCaseMetadataElement> Tests => TestCaseMetadataSource.Tests;

            public static SchemaMetadataSource SchemaMetadataSource { get; }

            static MetadataSources()
            {
                TestCaseMetadataSource = new TestCaseMetadataSource();

                var requiredContexts = TestCaseMetadataSource.Tests
                    .SelectMany(x => x.Arrange.Contexts)
                    .ToHashSet();

                SchemaMetadataSource = new SchemaMetadataSource(requiredContexts);
            }
        }
    }
}
