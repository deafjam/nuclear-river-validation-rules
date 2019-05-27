using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.ValidationRules.Storage.SchemaInitializer
{
    public sealed class SqlSchemaService
    {
        private readonly DataConnection _dataConnection;

        public SqlSchemaService(DataConnection dataConnection) => _dataConnection = dataConnection;

        public void CreateTables(IEnumerable<Type> dataObjectTypes)
        {
            foreach (var dataObjectType in dataObjectTypes)
            {
                var schemaManager = new SchemaManager(dataObjectType);
                schemaManager.CreateSchema(_dataConnection);

                var tableManager = TableManager.Create(dataObjectType);
                tableManager.CreateTable(_dataConnection);

                var indexManager = new IndexManager(dataObjectType);
                indexManager.CreateIndices(_dataConnection);
            }
        }

        public void DropTables(IEnumerable<TableSchema> tables)
        {
            foreach (var table in tables)
            {
                _dataConnection.DropTable<object>(tableName: table.TableName, schemaName: table.SchemaName);
            }
        }

        public IReadOnlyList<TableSchema> AllTables()
        {
            var dataProvider = DataConnection.GetDataProvider(_dataConnection.ConfigurationString);
            var schemaProvider = dataProvider.GetSchemaProvider();
            return schemaProvider.GetSchema(_dataConnection).Tables;
        }

        private abstract class TableManager
        {
            public static TableManager Create(Type dataObjectType)
            {
                var managerType = typeof(TableManagerImpl<>).MakeGenericType(dataObjectType);
                return (TableManager)Activator.CreateInstance(managerType);
            }

            public abstract void CreateTable(DataConnection db);

            private sealed class TableManagerImpl<T> : TableManager where T : class
            {
                public override void CreateTable(DataConnection db) => db.CreateTable<T>();
            }
        }

        private sealed class IndexManager
        {
            private readonly Type _dataObjectType;

            public IndexManager(Type dataObjectType) => _dataObjectType = dataObjectType;

            public void CreateIndices(DataConnection db)
            {
                var table = db.MappingSchema.GetAttribute<TableAttribute>(_dataObjectType);
                var indices = db.MappingSchema.GetAttributes<SchemaExtensions.IndexAttribute>(_dataObjectType);
                foreach (var index in indices)
                {
                    var command = db.CreateCommand();

                    var schemaName = table.Schema ?? "dbo";
                    var tableName = table.Name ?? _dataObjectType.Name;
                    var name = index.Name ?? string.Join("_", index.Fields.Select(x => x.Name));
                    var unique = index.Unique ? "UNIQUE" : null;
                    var clustered = index.Clustered ? "CLUSTERED" : null;

                    command.CommandText = $"CREATE {unique} {clustered} INDEX [IX_{tableName}_{name}] ON [{schemaName}].[{tableName}] "
                                          + $"({string.Join(", ", index.Fields.Select(x => "[" + x.Name + "]"))})"
                                          + (index.Include.Any() ? $" INCLUDE ({string.Join(", ", index.Include.Select(x => "[" + x.Name + "]"))})" : string.Empty);
                    command.ExecuteNonQuery();
                }
            }
        }

        private sealed class SchemaManager
        {
            private readonly Type _dataObjectType;

            public SchemaManager(Type dataObjectType) => _dataObjectType = dataObjectType;

            public void CreateSchema(DataConnection db)
            {
                var table = db.MappingSchema.GetAttribute<TableAttribute>(_dataObjectType);
                var schema = table.Schema ?? "dbo";

                var command = db.CreateCommand();
                command.CommandText = $"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{schema}') EXEC('CREATE SCHEMA {schema}')";
                command.ExecuteNonQuery();
            }
        }
	}
}
