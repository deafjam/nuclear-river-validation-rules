using System.Linq;

using NuClear.Messaging.API.Processing.Actors.Accumulators;
using NuClear.Replication.Core;
using NuClear.Replication.OperationsProcessing;

namespace NuClear.ValidationRules.OperationsProcessing.AggregatesFlow
{
    public sealed class AggregatesFlowAccumulator : MessageProcessingContextAccumulatorBase<AggregatesFlow, EventMessage, AggregatableMessage<ICommand>>
    {
        private readonly ICommandFactory<EventMessage> _commandFactory = new AggregatesCommandFactory();

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