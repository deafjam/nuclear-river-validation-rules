using NuClear.Jobs;
using NuClear.OperationsLogging.API;
using NuClear.Replication.Core;
using NuClear.Security.API.Auth;
using NuClear.Security.API.Context;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Writings;
using NuClear.ValidationRules.Hosting.Common;
using NuClear.ValidationRules.OperationsProcessing;
using NuClear.ValidationRules.OperationsProcessing.Facts.AmsFactsFlow;
using NuClear.ValidationRules.Replication.Events;
using Quartz;
using System;
using System.Linq;
using SystemStatus = NuClear.ValidationRules.Storage.Model.Facts.SystemStatus;

namespace NuClear.ValidationRules.Replication.Host.Jobs
{
    public sealed class HeartbeatJob : TaskServiceJobBase
    {
        private static readonly TimeSpan AmsSyncInterval = TimeSpan.FromMinutes(1);

        private readonly KafkaMessageFlowInfoProvider _kafkaMessageFlowInfoProvider;
        private readonly IQuery _query;
        private readonly IRepository<SystemStatus> _repository;
        private readonly IEventLogger _eventLogger;

        public HeartbeatJob(IUserContextManager userContextManager,
                            IUserAuthenticationService userAuthenticationService,
                            IUserAuthorizationService userAuthorizationService,
                            IJobExecutionObserver jobExecutionObserver,
                            KafkaMessageFlowInfoProvider kafkaMessageFlowInfoProvider,
                            IQuery query,
                            IRepository<SystemStatus> repository,
                            IEventLogger eventLogger)
            : base(userContextManager, userAuthenticationService, userAuthorizationService, jobExecutionObserver)
        {
            _kafkaMessageFlowInfoProvider = kafkaMessageFlowInfoProvider;
            _query = query;
            _repository = repository;
            _eventLogger = eventLogger;
        }

        protected override void ExecuteInternal(IJobExecutionContext context)
        {
            SendAmsHeartbeat();
        }

        private void SendAmsHeartbeat()
        {
            var result = _kafkaMessageFlowInfoProvider.TryGetFlowLastMessage(AmsFactsFlow.Instance); 
            if (result == null || result.IsPartitionEOF)
            {
                return;
            }

            var utcNow = DateTime.UtcNow;
            var amsUtcNow = result.Timestamp.UtcDateTime;
            var amsIsDown = (utcNow - amsUtcNow).Duration() > AmsSyncInterval;

            var amsSystemStatus = _query.For<SystemStatus>().Single(x => x.Id == SystemStatus.SystemId.Ams);
            if (amsSystemStatus.SystemIsDown == amsIsDown)
            {
                return;
            }

            amsSystemStatus.SystemIsDown = amsIsDown;
            _repository.Update(amsSystemStatus);
            _repository.Save();
            _eventLogger.Log<IEvent>(new[] { new FlowEvent(AmsFactsFlow.Instance, new DataObjectUpdatedEvent(typeof(SystemStatus), new[] { SystemStatus.SystemId.Ams })) });
        }
    }
}