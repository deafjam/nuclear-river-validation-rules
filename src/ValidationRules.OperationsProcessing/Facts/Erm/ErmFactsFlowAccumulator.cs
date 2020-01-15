using System.Linq;
using NuClear.Messaging.API.Processing.Actors.Accumulators;
using NuClear.OperationsTracking.API.UseCases;
using NuClear.Replication.Core;
using NuClear.Replication.OperationsProcessing;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.Erm
{
    public sealed class ErmFactsFlowAccumulator : MessageProcessingContextAccumulatorBase<ErmFactsFlow, TrackedUseCase, AggregatableMessage<ICommand>>
    {
        private readonly ICommandFactory<TrackedUseCase> _commandFactory= new ErmFactsCommandFactory();

        protected override AggregatableMessage<ICommand> Process(TrackedUseCase trackedUseCase)
        {
            var commands = _commandFactory.CreateCommands(trackedUseCase).ToList();
            
            return new AggregatableMessage<ICommand>
            {
                TargetFlow = MessageFlow,
                Commands = commands,
            };
        }
    }
}