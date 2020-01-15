using System.Collections.Generic;
using NuClear.Replication.Core;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka
{
    public interface IBulkCommandFactory<in TMessage>
        where TMessage : class
    {
        IReadOnlyCollection<ICommand> CreateCommands(IReadOnlyCollection<TMessage> messages);
    }
}
