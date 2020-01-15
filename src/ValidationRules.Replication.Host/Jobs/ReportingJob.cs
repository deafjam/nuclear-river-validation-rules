using Microsoft.ServiceBus;
using NuClear.Jobs;
using NuClear.Messaging.API.Flows;
using NuClear.Replication.OperationsProcessing.Telemetry;
using NuClear.Replication.OperationsProcessing.Transports.ServiceBus.Factories;
using NuClear.Security.API.Auth;
using NuClear.Security.API.Context;
using NuClear.Telemetry;
using NuClear.ValidationRules.Hosting.Common;
using NuClear.ValidationRules.OperationsProcessing.AggregatesFlow;
using NuClear.ValidationRules.OperationsProcessing.MessagesFlow;
using Quartz;
using System;
using System.Linq;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;
using NuClear.ValidationRules.OperationsProcessing.Facts.Erm;

namespace NuClear.ValidationRules.Replication.Host.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class ReportingJob : TaskServiceJobBase
    {
        private readonly ITelemetryPublisher _telemetry;
        private readonly IServiceBusSettingsFactory _serviceBusSettingsFactory;
        private readonly KafkaMessageFlowInfoProvider _kafkaMessageFlowInfoProvider;

        public ReportingJob(
            ITelemetryPublisher telemetry,
            IServiceBusSettingsFactory serviceBusSettingsFactory,
            KafkaMessageFlowInfoProvider kafkaMessageFlowInfoProvider,
            IUserContextManager userContextManager,
            IUserAuthenticationService userAuthenticationService,
            IUserAuthorizationService userAuthorizationService,
            IJobExecutionObserver jobExecutionObserver)
            : base(userContextManager, userAuthenticationService, userAuthorizationService, jobExecutionObserver)
        {
            _kafkaMessageFlowInfoProvider = kafkaMessageFlowInfoProvider;
            _telemetry = telemetry;
            _serviceBusSettingsFactory = serviceBusSettingsFactory;
        }

        protected override void ExecuteInternal(IJobExecutionContext context)
        {
            WithinErrorLogging(ReportMemoryUsage);
            WithinErrorLogging(ReportServiceBusQueueLength<ErmFactsFlow, PrimaryProcessingQueueLengthIdentity>);
            WithinErrorLogging(ReportServiceBusQueueLength<AggregatesFlow, FinalProcessingAggregateQueueLengthIdentity>);
            WithinErrorLogging(ReportServiceBusQueueLength<MessagesFlow, MessagesQueueLengthIdentity>);
            WithinErrorLogging(ReportKafkaOffset<AmsFactsFlow, AmsFactsQueueLengthIdentity>);
        }

        private void WithinErrorLogging(Action action)
        {
            action.Invoke();
        }

        private void ReportMemoryUsage()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            _telemetry.Publish<ProcessPrivateMemorySizeIdentity>(process.PrivateMemorySize64);
            _telemetry.Publish<ProcessWorkingSetIdentity>(process.WorkingSet64);
        }

        private void ReportServiceBusQueueLength<TFlow, TTelemetryIdentity>()
            where TFlow : MessageFlowBase<TFlow>, new()
            where TTelemetryIdentity : TelemetryIdentityBase<TTelemetryIdentity>, new()
        {
            var flow = MessageFlowBase<TFlow>.Instance;
            var settings = _serviceBusSettingsFactory.CreateReceiverSettings(flow);
            var manager = NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
            var subscription = manager.GetSubscription(settings.TransportEntityPath, flow.Id.ToString());
            _telemetry.Publish<TTelemetryIdentity>(subscription.MessageCountDetails.ActiveMessageCount);
        }

        private void ReportKafkaOffset<TFlow, TTelemetryIdentity>()
            where TFlow : MessageFlowBase<TFlow>, new()
            where TTelemetryIdentity : TelemetryIdentityBase<TTelemetryIdentity>, new()
        {
            var flow = MessageFlowBase<TFlow>.Instance;

            var lag = _kafkaMessageFlowInfoProvider.GetFlowStats(flow).Single().Lag;
            _telemetry.Publish<TTelemetryIdentity>(lag);
        }

        private sealed class MessagesQueueLengthIdentity : TelemetryIdentityBase<MessagesQueueLengthIdentity>
        {
            public override int Id => 0;
            public override string Description => nameof(MessagesQueueLengthIdentity);
        }

        private sealed class AmsFactsQueueLengthIdentity : TelemetryIdentityBase<AmsFactsQueueLengthIdentity>
        {
            public override int Id => 0;

            public override string Description => nameof(AmsFactsQueueLengthIdentity);
        }

    }
}