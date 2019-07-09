using System.Linq;
using NuClear.Messaging.API.Processing.Actors.Accumulators;
using NuClear.Replication.Core;
using NuClear.Replication.OperationsProcessing;

namespace NuClear.ValidationRules.OperationsProcessing.MessagesFlow
{
    public sealed class MessagesFlowAccumulator : MessageProcessingContextAccumulatorBase<MessagesFlow, EventMessage, AggregatableMessage<ICommand>>
    {
        private readonly ICommandFactory<EventMessage> _commandFactory = new MessagesFlowCommandFactory();

        protected override AggregatableMessage<ICommand> Process(EventMessage message)
        {
            var commands = _commandFactory.CreateCommands(message).ToList();
            
            return new AggregatableMessage<ICommand>
            {
                TargetFlow = MessageFlow,
                Commands = commands
            };
        }
    }
}