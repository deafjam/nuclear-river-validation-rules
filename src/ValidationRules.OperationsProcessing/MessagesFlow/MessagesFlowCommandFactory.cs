using System;
using System.Collections.Generic;
using NuClear.Replication.Core;
using NuClear.Replication.OperationsProcessing;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;

namespace NuClear.ValidationRules.OperationsProcessing.MessagesFlow
{
    internal sealed class MessagesFlowCommandFactory : ICommandFactory<EventMessage>
    {
        public IEnumerable<ICommand> CreateCommands(EventMessage message)
        {
            switch (message.Event)
            {
                case AmsStateIncrementedEvent amsStateIncrementedEvent:
                    yield return new StoreAmsStateCommand(amsStateIncrementedEvent.State);
                    break;

                case ErmStateIncrementedEvent ermStateIncrementedEvent:
                    yield return new StoreErmStateCommand(ermStateIncrementedEvent.States);
                    break;

                case DelayLoggedEvent delayLoggedEvent:
                    yield return new LogDelayCommand(delayLoggedEvent.EventTime);
                    break;

                case ResultOutdatedEvent resultOutdatedEvent:
                    yield return new RecalculateValidationRuleCompleteCommand(resultOutdatedEvent.Rule);
                    break;

                case ResultPartiallyOutdatedEvent resultPartiallyOutdatedEvent:
                    yield return new RecalculateValidationRulePartiallyCommand(resultPartiallyOutdatedEvent.Rule, resultPartiallyOutdatedEvent.OrderIds);
                    break;

                default:
                    throw new ArgumentException($"Unexpected event '{message.Event}'", nameof(message.Event));
            }
        }
    }
}