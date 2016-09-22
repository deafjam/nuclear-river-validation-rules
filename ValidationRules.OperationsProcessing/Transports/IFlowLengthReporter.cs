﻿using System.Collections.Generic;

using NuClear.Messaging.API.Flows;

namespace NuClear.ValidationRules.OperationsProcessing.Transports
{
    public interface IFlowLengthReporter
    {
        IReadOnlyCollection<IMessageFlow> SeriviceBusFlows { get; }
        IReadOnlyCollection<IMessageFlow> SqlFlows { get; }
        void ReportFlowLength(IMessageFlow flow, int length);
    }
}