using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication
{
    public abstract class DataChangesHandler<T> : IDataChangesHandler<T>
    {
        private readonly IRuleInvalidator _invalidator;

        protected DataChangesHandler(IRuleInvalidator invalidator) => _invalidator = invalidator;

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<T> dataObjects)
            => _invalidator.Invalidate(dataObjects);

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<T> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<T> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<T> dataObjects) => Array.Empty<IEvent>();

        protected interface IRuleInvalidator
        {
            IReadOnlyCollection<IEvent> Invalidate(IReadOnlyCollection<T> dataObjects);
        }

        protected sealed class RuleInvalidator : IRuleInvalidator, IEnumerable
        {
            private readonly List<MessageTypeCode> _outdated = new List<MessageTypeCode>();
            private readonly Dictionary<MessageTypeCode, Func<IReadOnlyCollection<T>, IEnumerable<long>>> _partiallyOutdated = new Dictionary<MessageTypeCode, Func<IReadOnlyCollection<T>, IEnumerable<long>>>();

            public void Add(MessageTypeCode ruleCode)
                => _outdated.Add(ruleCode);

            public void Add(MessageTypeCode ruleCode, Func<IReadOnlyCollection<T>, IEnumerable<long>> func)
                => _partiallyOutdated.Add(ruleCode, func);

            IReadOnlyCollection<IEvent> IRuleInvalidator.Invalidate(IReadOnlyCollection<T> dataObjects)
                => _outdated.Select(x => new ResultOutdatedEvent(x)).Cast<IEvent>()
                    .Concat(_partiallyOutdated.Select(x => new ResultPartiallyOutdatedEvent(x.Key, x.Value(dataObjects).ToList())))
                    .ToList();

            // нужно только для работы collection initializers
            IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
        }
    }
}
