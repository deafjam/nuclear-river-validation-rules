using System.Collections.Generic;

using NuClear.Messaging.API;
using NuClear.Replication.Core;

namespace NuClear.ValidationRules.OperationsProcessing
{
    public interface ICommandFactory<in TMessage>
        where TMessage : class, IMessage
    {
        IEnumerable<ICommand> CreateCommands(TMessage message);
    }
}
