﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NuClear.Messaging.API.Processing;
using NuClear.Messaging.API.Processing.Actors.Handlers;
using NuClear.Messaging.API.Processing.Stages;
using NuClear.OperationsLogging.API;
using NuClear.Replication.Core;
using NuClear.Replication.Core.Commands;
using NuClear.Replication.OperationsProcessing;
using NuClear.Tracing.API;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.Ams
{
    public sealed class AmsFactsFlowHandler : IMessageProcessingHandler
    {
        private readonly IDataObjectsActorFactory _dataObjectsActorFactory;
        private readonly SyncEntityNameActor _syncEntityNameActor;
        private readonly IEventLogger _eventLogger;
        private readonly ITracer _tracer;
        private readonly AmsFactsFlowTelemetryPublisher _telemetryPublisher;

        private readonly TransactionOptions _transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.Zero
            };

        public AmsFactsFlowHandler(
            IDataObjectsActorFactory dataObjectsActorFactory,
            SyncEntityNameActor syncEntityNameActor,
            IEventLogger eventLogger,
            AmsFactsFlowTelemetryPublisher telemetryPublisher,
            ITracer tracer)
        {
            _dataObjectsActorFactory = dataObjectsActorFactory;
            _syncEntityNameActor = syncEntityNameActor;
            _eventLogger = eventLogger;
            _telemetryPublisher = telemetryPublisher;
            _tracer = tracer;
        }

        public IEnumerable<StageResult> Handle(IReadOnlyDictionary<Guid, List<IAggregatableMessage>> processingResultsMap)
        {
            try
            {
                var commands = processingResultsMap.SelectMany(x => x.Value)
                    .Cast<AggregatableMessage<ICommand>>()
                    .SelectMany(x => x.Commands)
                    .ToList();

                using (var transaction = new TransactionScope(TransactionScopeOption.Required, _transactionOptions))
                {
                    var replaceEvents = Handle(commands.OfType<IReplaceDataObjectCommand>().ToList())
                                        .Select(x => new FlowEvent(AmsFactsFlow.Instance, x)).ToList();

                    using (new TransactionScope(TransactionScopeOption.Suppress))
                        _eventLogger.Log<IEvent>(replaceEvents);

                    transaction.Complete();
                }
                
                var stateEvents = Handle(commands.OfType<IncrementAmsStateCommand>().ToList())
                    .Select(x => new FlowEvent(AmsFactsFlow.Instance, x)).ToList();
                _eventLogger.Log<IEvent>(stateEvents);

                return processingResultsMap.Keys.Select(bucketId => MessageProcessingStage.Handling.ResultFor(bucketId).AsSucceeded());
            }
            catch (Exception ex)
            {
                _tracer.Error(ex, "Error when import facts for AMS");
                return processingResultsMap.Keys.Select(bucketId => MessageProcessingStage.Handling.ResultFor(bucketId).AsFailed().WithExceptions(ex));
            }
        }

        private IEnumerable<IEvent> Handle(IReadOnlyCollection<IncrementAmsStateCommand> commands)
        {
            if (commands.Count == 0)
            {
                return Enumerable.Empty<IEvent>();
            }

            var eldestEventTime = commands.Min(x => x.State.UtcDateTime);
            var delta = DateTime.UtcNow - eldestEventTime;
            _telemetryPublisher.Delay((int)delta.TotalMilliseconds);

            var maxAmsState = commands.Select(x => x.State).OrderByDescending(x => x.Offset).First();
            return new IEvent[]
            {
                new AmsStateIncrementedEvent(maxAmsState),
                new DelayLoggedEvent(DateTime.UtcNow)
            };
        }

        private IEnumerable<IEvent> Handle(IReadOnlyCollection<IReplaceDataObjectCommand> commands)
        {
            if (commands.Count == 0)
            {
                return Enumerable.Empty<IEvent>();
            }

            var dataObjectTypes = commands.Select(x => x.DataObjectType).ToHashSet();
            var actors = _dataObjectsActorFactory.Create(dataObjectTypes);

            var eventCollector = new FactsEventCollector();
            foreach (var actor in actors)
            {
                var events = actor.ExecuteCommands(commands);
                eventCollector.Add(events);
            }

            _syncEntityNameActor.ExecuteCommands(commands);

            return eventCollector.Events();
        }
    }
}
