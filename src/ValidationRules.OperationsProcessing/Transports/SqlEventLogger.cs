using System;
using System.Collections.Generic;
using System.Linq;
using NuClear.OperationsLogging.API;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.OperationsProcessing.Transports;
using NuClear.ValidationRules.Storage.Model.Events;

namespace NuClear.ValidationRules.OperationsProcessing.Transports
{
    public sealed class SqlEventLogger : IEventLogger
    {
        private readonly IXmlEventSerializer _serializer;
        private readonly IBulkRepository<EventRecord> _repository;

        public SqlEventLogger(IXmlEventSerializer serializer, IBulkRepository<EventRecord> repository)
        {
            _serializer = serializer;
            _repository = repository;
        }

        public void Log<TEvent>(IReadOnlyCollection<TEvent> events) =>
            _repository.Create(events.Select(Serialize));

        private EventRecord Serialize<TEvent>(TEvent evt) =>
            evt switch
            {
                FlowEvent flowEvent => new EventRecord
                {
                    Flow = flowEvent.Flow.Id,
                    Content = _serializer.Serialize(flowEvent.Event).ToString(),
                },
                _ => throw new ArgumentException($"Unsupported event type {evt.GetType()}"),
            };
    }
}
