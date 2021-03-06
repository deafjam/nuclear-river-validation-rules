﻿using System.Linq;
using NuClear.Messaging.API.Processing.Actors.Accumulators;
using NuClear.OperationsProcessing.Transports.Kafka;
using NuClear.Replication.Core;
using NuClear.Replication.OperationsProcessing;
using NuClear.ValidationRules.Hosting.Common.Settings;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.RulesetFactsFlow
{
    public sealed class RulesetFactsFlowAccumulator : MessageProcessingContextAccumulatorBase<RulesetFactsFlow, KafkaMessage, AggregatableMessage<ICommand>>
    {
        private readonly ICommandFactory<KafkaMessage> _commandFactory;

        public RulesetFactsFlowAccumulator()
        {
            _commandFactory = new RulesetFactsCommandFactory();
        }

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
