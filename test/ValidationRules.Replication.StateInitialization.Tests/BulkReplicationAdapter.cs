using NuClear.DataTest.Metamodel.Dsl;
using NuClear.Replication.Core;
using NuClear.Replication.Core.Tenancy;
using NuClear.StateInitialization.Core.Actors;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed class BulkReplicationAdapter<T> : ITestAction
        where T : IKey, new()
    {
        private readonly T _key;
        private readonly ITenantConnectionStringSettings _connectionStringSettings;

        public BulkReplicationAdapter()
        {
            _key = new T();
            _connectionStringSettings = new RunnerConnectionStringSettings();
        }

        public void Act()
        {
            var bulkReplicationActor = new BulkReplicationActor(_connectionStringSettings);
            bulkReplicationActor.ExecuteCommands(new[] { _key.Command });
        }
    }
}