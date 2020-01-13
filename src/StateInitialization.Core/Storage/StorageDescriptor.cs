using System;

using LinqToDB.Mapping;

using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.StateInitialization.Core.Storage
{
    public sealed class StorageDescriptor
    {
        private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(30);

        public StorageDescriptor(IConnectionStringIdentity connectionStringIdentity, MappingSchema mappingSchema, TimeSpan? commandTimeout = null)
        {
            ConnectionStringIdentity = connectionStringIdentity;
            MappingSchema = mappingSchema;
            CommandTimeout = commandTimeout ?? DefaultCommandTimeout;
        }

        public IConnectionStringIdentity ConnectionStringIdentity { get; }
        public MappingSchema MappingSchema { get; }
        public TimeSpan CommandTimeout { get; }
    }
}