using NuClear.Jobs;
using NuClear.Messaging.API.Flows.Metadata;
using NuClear.Messaging.API.Processing.Processors;
using NuClear.Messaging.API.Processing.Stages;
using NuClear.Metamodeling.Provider;
using NuClear.OperationsProcessing.API;
using NuClear.OperationsProcessing.API.Metadata;
using NuClear.OperationsProcessing.API.Primary;
using NuClear.Security.API.Auth;
using NuClear.Security.API.Context;
using NuClear.Telemetry;
using NuClear.Telemetry.Probing;
using Quartz;
using System;

namespace NuClear.ValidationRules.Replication.Host.Jobs
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public sealed class ProcessingJob : TaskServiceJobBase
    {
        private readonly IMetadataProvider _metadataProvider;
        private readonly IMessageFlowProcessorFactory _messageFlowProcessorFactory;
        private readonly ITelemetryPublisher _telemetry;

        public ProcessingJob(
            IMetadataProvider metadataProvider,
            IMessageFlowProcessorFactory messageFlowProcessorFactory,
            IUserContextManager userContextManager,
            IUserAuthenticationService userAuthenticationService,
            IUserAuthorizationService userAuthorizationService,
            ITelemetryPublisher telemetry,
            IJobExecutionObserver jobExecutionObserver)
            : base(userContextManager, userAuthenticationService, userAuthorizationService, jobExecutionObserver)
        {
            _metadataProvider = metadataProvider;
            _messageFlowProcessorFactory = messageFlowProcessorFactory;
            _telemetry = telemetry;
        }

        public int FailCount { get; set; }
        public int BatchSize { get; set; }
        public string Flow { get; set; }

        private int BatchSizeAccordingFailures
            => Math.Max(BatchSize >> FailCount, 1);

        protected override void ExecuteInternal(IJobExecutionContext context)
        {
            if (string.IsNullOrEmpty(Flow))
            {
                var msg = $"Required job arg {nameof(Flow)} is not specified, check job config";
                throw new InvalidOperationException(msg);
            }

            try
            {
                using (Probe.Create(Flow))
                {
                    ProcessFlow();
                }

                DecrementFailCount(context);
            }
            catch
            {
                IncrementFailCount(context);
            }
            finally
            {
                var reports = DefaultReportSink.Instance.ConsumeReports();
                foreach (var report in reports)
                {
                    _telemetry.Trace("ProbeReport", report);
                }
            }
        }

        private void IncrementFailCount(IJobExecutionContext context)
        {
            context.JobDetail.JobDataMap.Put("FailCount", Math.Min(FailCount + 1, 31));
        }

        private void DecrementFailCount(IJobExecutionContext context)
        {
            context.JobDetail.JobDataMap.Put("FailCount", Math.Max(FailCount - 1, 0));
        }

        private void ProcessFlow()
        {
            if (!_metadataProvider.TryGetMetadata(Flow.AsPrimaryProcessingFlowId(), out MessageFlowMetadata messageFlowMetadata))
            {
                var msg = "Unsupported flow specified for processing: " + Flow;
                throw new InvalidOperationException(msg);
            }

            var processorSettings = new PerformedOperationsPrimaryFlowProcessorSettings
            {
                MessageBatchSize = BatchSizeAccordingFailures,
                AppropriatedStages = new[]
                {
                    MessageProcessingStage.Transformation,
                    MessageProcessingStage.Accumulation,
                    MessageProcessingStage.Handling
                },
                FirstFaultTolerantStage = MessageProcessingStage.None
            };

            var messageFlowProcessor = _messageFlowProcessorFactory.CreateSync<IPerformedOperationsFlowProcessorSettings>(messageFlowMetadata, processorSettings);

            try
            {
                messageFlowProcessor.Process();
            }
            finally
            {
                messageFlowProcessor?.Dispose();
            }
        }
    }
}