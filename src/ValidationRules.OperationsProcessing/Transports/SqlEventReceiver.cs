using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NuClear.Messaging.API;
using NuClear.Messaging.API.Flows;
using NuClear.Messaging.API.Receivers;
using NuClear.OperationsProcessing.API.Primary;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.OperationsProcessing;
using NuClear.Replication.OperationsProcessing.Transports;
using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Model.Events;

namespace NuClear.ValidationRules.OperationsProcessing.Transports
{
    public sealed class SqlEventReceiver<TFlow> : IMessageReceiver
        where TFlow : MessageFlowBase<TFlow>, new()
    {
        private readonly IPerformedOperationsReceiverSettings _settings;
        private readonly IXmlEventSerializer _eventSerializer;
        private readonly IQuery _query;
        private readonly IBulkRepository<EventRecord> _bulkRepository;
        private readonly SqlEventReceiverConfiguration _configuration;

        public SqlEventReceiver(
            IPerformedOperationsReceiverSettings settings,
            IXmlEventSerializer eventSerializer,
            IQuery query,
            IBulkRepository<EventRecord> bulkRepository,
            SqlEventReceiverConfiguration configuration)
        {
            _settings = settings;
            _eventSerializer = eventSerializer;
            _query = query;
            _bulkRepository = bulkRepository;
            _configuration = configuration;
        }

        public IReadOnlyList<IMessage> Peek()
        {
            var ids = _configuration.GetConsumableFlows(MessageFlowBase<TFlow>.Instance)
                .Select(x => x.Id);

            var records = _query.For<EventRecord>()
                .Where(x => ids.Contains(x.Flow))
                .OrderBy(x => x.Id)
                .Take(_settings.BatchSize)
                .ToArray();

            return records.Select(Deserialize).ToList();
        }

        public void Complete(
            IEnumerable<IMessage> successfullyProcessedMessages,
            IEnumerable<IMessage> failedProcessedMessages)
        {
            var processedRecords = successfullyProcessedMessages
                .Select(x => new EventRecord {Id = ToLong(x.Id)});
            _bulkRepository.Delete(processedRecords);
        }

        private EventMessage Deserialize(
            EventRecord record)
        {
            return new EventMessage(
                ToGuid(record.Id),
                _eventSerializer.Deserialize(XElement.Parse(record.Content)));
        }

        private static Guid ToGuid(long id)
        {
            var data = new byte[16];
            Array.Copy(BitConverter.GetBytes(id), data, 8);
            return new Guid(data);
        }

        private static long ToLong(Guid id)
        {
            return BitConverter.ToInt64(id.ToByteArray(), 0);
        }
    }
}
