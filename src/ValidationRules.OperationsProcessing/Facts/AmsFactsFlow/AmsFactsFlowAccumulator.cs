﻿using System.Linq;
using NuClear.Messaging.API.Processing.Actors.Accumulators;
using NuClear.OperationsProcessing.Transports.Kafka;
using NuClear.Replication.Core;
using NuClear.Replication.OperationsProcessing;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.AmsFactsFlow
{
    public sealed class AmsFactsFlowAccumulator : MessageProcessingContextAccumulatorBase<AmsFactsFlow, KafkaMessage, AggregatableMessage<ICommand>>
    {
        private readonly ICommandFactory<KafkaMessage> _commandFactory = new AmsFactsCommandFactory();

        protected override AggregatableMessage<ICommand> Process(KafkaMessage kafkaMessage)
        {
            var commands = _commandFactory.CreateCommands(kafkaMessage).ToList();
            
            return new AggregatableMessage<ICommand>
            {
                TargetFlow = MessageFlow,
                Commands = commands,
            };
        }
    }
}