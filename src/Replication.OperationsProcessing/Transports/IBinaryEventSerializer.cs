using NuClear.Replication.Core;

namespace NuClear.Replication.OperationsProcessing.Transports
{
    public interface IBinaryEventSerializer
    {
        byte[] Serialize(IEvent @event);

        IEvent Deserialize(byte[] message);
    }
}