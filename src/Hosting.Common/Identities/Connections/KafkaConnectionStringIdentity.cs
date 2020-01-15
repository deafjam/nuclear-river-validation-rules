using NuClear.Model.Common;
using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.River.Hosting.Common.Identities.Connections
{
    public sealed class KafkaConnectionStringIdentity : IdentityBase<KafkaConnectionStringIdentity>, IConnectionStringIdentity
    {
        public override int Id => 19;

        public override string Description => "Apache Kafka connection string identity";
    }
}