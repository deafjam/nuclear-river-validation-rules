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
using NuClear.Telemetry.Probing;
using NuClear.Tracing.API;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.Erm
{
    public sealed class ErmFactsFlowHandler : IMessageProcessingHandler
    {
        private readonly IDataObjectsActorFactory _dataObjectsActorFactory;
        private readonly SyncEntityNameActor _syncEntityNameActor;
        private readonly IEventLogger _eventLogger;
        private readonly ITracer _tracer;
        private readonly ErmFactsFlowTelemetryPublisher _telemetryPublisher;
        private readonly TransactionOptions _transactionOptions;

        public ErmFactsFlowHandler(
            IDataObjectsActorFactory dataObjectsActorFactory,
            SyncEntityNameActor syncEntityNameActor,
            IEventLogger eventLogger,
            ErmFactsFlowTelemetryPublisher telemetryPublisher,
            ITracer tracer)
        {
            _dataObjectsActorFactory = dataObjectsActorFactory;
            _syncEntityNameActor = syncEntityNameActor;
            _eventLogger = eventLogger;
            _telemetryPublisher = telemetryPublisher;
            _tracer = tracer;
            _transactionOptions = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.Zero };
        }

        public IEnumerable<StageResult> Handle(IReadOnlyDictionary<Guid, List<IAggregatableMessage>> processingResultsMap)
        {
            try
            {
                var commands = processingResultsMap.SelectMany(x => x.Value).Cast<AggregatableMessage<ICommand>>().SelectMany(x => x.Commands).ToList();
                
                using (Probe.Create("ETL1 Transforming"))
                using (var transaction = new TransactionScope(TransactionScopeOption.Required, _transactionOptions))
                {
                    var syncEvents = Handle(commands.OfType<ISyncDataObjectCommand>().ToList())
                                     .Select(x => new FlowEvent(ErmFactsFlow.Instance, x)).ToList();
                    
                    using (new TransactionScope(TransactionScopeOption.Suppress))
                        _eventLogger.Log<IEvent>(syncEvents);

                    transaction.Complete();
                }

                var stateEvents = Handle(commands.OfType<IncrementErmStateCommand>().ToList())
                    .Select(x => new FlowEvent(ErmFactsFlow.Instance, x)).ToList();
                _eventLogger.Log<IEvent>(stateEvents);
                
                return processingResultsMap.Keys.Select(bucketId => MessageProcessingStage.Handling.ResultFor(bucketId).AsSucceeded());
            }
            catch (Exception ex)
            {
                _tracer.Error(ex, "Error when import facts for ERM");
                return processingResultsMap.Keys.Select(bucketId => MessageProcessingStage.Handling.ResultFor(bucketId).AsFailed().WithExceptions(ex));
            }
        }

        private IEnumerable<IEvent> Handle(IReadOnlyCollection<IncrementErmStateCommand> commands)
        {
            if (commands.Count == 0)
            {
                yield break;
            }

            var eldestEventTime = commands.SelectMany(x => x.States).Min(x => x.UtcDateTime);
            var delta = DateTime.UtcNow - eldestEventTime;
            _telemetryPublisher.Delay((int)delta.TotalMilliseconds);

            yield return new ErmStateIncrementedEvent(commands.SelectMany(x => x.States));
            yield return new DelayLoggedEvent(DateTime.UtcNow);
        }

        private IEnumerable<IEvent> Handle(IReadOnlyCollection<ISyncDataObjectCommand> commands)
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
                using (Probe.Create($"ETL1 {actor.GetType().GetFriendlyName()}"))
                {
                    var events = actor.ExecuteCommands(commands);
                    eventCollector.Add(events);
                }
            }

            _syncEntityNameActor.ExecuteCommands(commands);

            return eventCollector.Events();
        }
    }
}
