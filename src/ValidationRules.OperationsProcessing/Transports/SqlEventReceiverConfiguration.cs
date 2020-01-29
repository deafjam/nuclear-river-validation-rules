using System;
using System.Collections.Generic;
using NuClear.Messaging.API.Flows;
using NuClear.ValidationRules.OperationsProcessing.Facts.AmsFactsFlow;
using NuClear.ValidationRules.OperationsProcessing.Facts.ErmFactsFlow;
using NuClear.ValidationRules.OperationsProcessing.Facts.RulesetFactsFlow;

namespace NuClear.ValidationRules.OperationsProcessing.Transports
{
    public sealed class SqlEventReceiverConfiguration
    {
        private static readonly IMessageFlow[] AggregatesFlowConsumeFlows =
            {AmsFactsFlow.Instance, RulesetFactsFlow.Instance, ErmFactsFlow.Instance};

        private static readonly IMessageFlow[] MessagesFlowConsumeFlows =
            {AggregatesFlow.AggregatesFlow.Instance};

        public IEnumerable<IMessageFlow> GetConsumableFlows(IMessageFlow flow)
        {
            switch (flow)
            {
                case AggregatesFlow.AggregatesFlow _:
                    return AggregatesFlowConsumeFlows;
                case MessagesFlow.MessagesFlow _:
                    return MessagesFlowConsumeFlows;
                default:
                    throw new ArgumentException($"Flow '{flow.GetType().Name}' has no configured consumed flows.");
            }
        }
    }
}