﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LinqToDB;
using LinqToDB.Data;

using Microsoft.Practices.Unity;

using NuClear.DataTest.Metamodel;
using NuClear.DataTest.Metamodel.Dsl;
using NuClear.DataTest.Runner.Comparer;
using NuClear.Metamodeling.Elements;
using NuClear.Metamodeling.Provider;

namespace NuClear.DataTest.Runner.Command
{
    public sealed class RunTestsCommand : ICommand
    {
        private readonly IUnityContainer _container;
        private readonly DataConnectionFactory _dataConnectionFactory;
        private readonly Dictionary<Uri, IMetadataElement> _testMetadata;
        private readonly Dictionary<string, SchemaMetadataElement> _schemaMetadata;

        public RunTestsCommand(IMetadataProvider metadataProvider, IUnityContainer container, DataConnectionFactory dataConnectionFactory)
        {
            _container = container;
            _dataConnectionFactory = dataConnectionFactory;

            _testMetadata = metadataProvider.GetMetadataSet<TestCaseMetadataIdentity>().Metadata;
            _schemaMetadata = metadataProvider.GetMetadataSet<SchemaMetadataIdentity>().Metadata.Values.Cast<SchemaMetadataElement>().ToDictionary(x => x.Context, x => x);
        }

        public void Execute()
        {
            foreach (var test in _testMetadata.Values.OfType<TestCaseMetadataElement>())
            {
                Arrange(test);
                Act(test);
                Assert(test);
            }
        }

        private void Assert(TestCaseMetadataElement test)
        {
            var message = new StringBuilder(test.Act.Target);
            var targetContextMetadata = _schemaMetadata[test.Act.Target];
            var hasErrors = false;
            using (var db = _dataConnectionFactory.CreateConnection(targetContextMetadata))
            {
                var actual = new DataConnectionReader(db);
                var expected = new DictionaryReader(test.Arrange.GetData(test.Act.Target));
                var comparer = new DataComparer(targetContextMetadata);

                foreach (var entityType in _schemaMetadata[test.Act.Target].Entities)
                {
                    var result = comparer.Compare(entityType, actual, expected);
                    var formatter = new MessageFormatter(entityType, targetContextMetadata);
                    var entityMessages = formatter.Format(result).ToArray();
                    if (entityMessages.Any())
                    {
                        message.AppendLine(entityType.Name);
                        entityMessages.Aggregate(message, (builder, s) => builder.Append("  ").AppendLine(s));
                        hasErrors = true;
                    }
                }
            }

            if (hasErrors)
            {
                throw new Exception(message.ToString());
            }
        }

        private void Act(TestCaseMetadataElement test)
        {
            using (var childContainer = _container.CreateChildContainer())
            {
                childContainer.RegisterInstance(test.Act);

                foreach (var actionType in test.Act.ActionTypes)
                {
                    var action = (ITestAction)childContainer.Resolve(actionType);
                    action.Act();
                }
            }
        }

        private void Arrange(TestCaseMetadataElement test)
        {
            foreach (var context in test.Arrange.Contexts)
            {
                var arrangeData = test.Act.Requirements.Contains(context)
                    ? new DictionaryReader(test.Arrange.GetData(context))
                    : new DictionaryReader(new Dictionary<Type, IReadOnlyCollection<object>>());

                var contextMetadata = _schemaMetadata[context];

                using (var db = _dataConnectionFactory.CreateConnection(contextMetadata))
                {
                    foreach (var entityType in contextMetadata.Entities)
                    {
                        var helperType = typeof(ArrangeHelper<>).MakeGenericType(entityType);
                        var helperInstance = (IArrangeHelper)Activator.CreateInstance(helperType);
                        helperInstance.Arrange(db, arrangeData);
                    }
                }
            }
        }

        private interface IArrangeHelper
        {
            void Arrange(DataConnection target, IReader source);
        }

        private class ArrangeHelper<T> : IArrangeHelper
            where T : class
        {
            public void Arrange(DataConnection target, IReader source)
            {
                target.GetTable<T>().Delete();
                target.GetTable<T>().BulkCopy(source.Read<T>());
            }
        }

        private class DataConnectionReader : IReader
        {
            private readonly DataConnection _db;

            public DataConnectionReader(DataConnection db)
            {
                _db = db;
            }

            public IReadOnlyCollection<T> Read<T>() where T : class
            {
                return _db.GetTable<T>().ToArray();
            }
        }

        private class DictionaryReader : IReader
        {
            private readonly IReadOnlyDictionary<Type, IReadOnlyCollection<object>> _data;

            public DictionaryReader(IReadOnlyDictionary<Type, IReadOnlyCollection<object>> data)
            {
                _data = data;
            }

            public IReadOnlyCollection<T> Read<T>() where T : class
            {
                IReadOnlyCollection<object> data;
                return _data.TryGetValue(typeof(T), out data)
                           ? data.Cast<T>().ToArray()
                           : new T[0];
            }
        }
    }
}
