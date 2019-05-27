using NuClear.DataTest.Engine.Command;
using NuClear.DataTest.Metamodel;
using NuClear.Metamodeling.Provider;
using NuClear.ValidationRules.Storage.SchemaInitializer;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests.Infrastructure
{
    public sealed class CreateDatabaseSchemataCommand : Command
    {
        private readonly DataConnectionFactory _dataConnectionFactory;
        private readonly IContextEntityTypesProvider _contextEntityTypesProvider;

        public CreateDatabaseSchemataCommand(IMetadataProvider metadataProvider,
                                             DataConnectionFactory dataConnectionFactory,
                                             IContextEntityTypesProvider contextEntityTypesProvider)
            : base(metadataProvider)
        {
            _dataConnectionFactory = dataConnectionFactory;
            _contextEntityTypesProvider = contextEntityTypesProvider;
        }

        protected override void Execute(SchemaMetadataElement metadataElement)
        {
            var entities = _contextEntityTypesProvider.GetTypesFromContext(metadataElement.Context);
            using (var dataConnection = _dataConnectionFactory.CreateConnection(metadataElement))
            {
                var sqlSchemaService = new SqlSchemaService(dataConnection);
                sqlSchemaService.CreateTables(entities);
            }
        }
    }
}
