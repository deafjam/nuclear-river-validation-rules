using NuClear.Jobs;
using NuClear.Security.API.Auth;
using NuClear.Security.API.Context;
using NuClear.Telemetry.Probing;
using NuClear.ValidationRules.Replication.Messages;
using NuClear.ValidationRules.Replication.Settings;
using Quartz;
using System;
using System.Transactions;

namespace NuClear.ValidationRules.Replication.Host.Jobs
{
    [DisallowConcurrentExecution]
    public class ArchivingJob : TaskServiceJobBase
    {
        private readonly ArchiveVersionsService _archiveVersionsService;
        private readonly IArchiveVersionsSettings _settings;
        private readonly TransactionOptions _transactionOptions;

        public ArchivingJob(
            ArchiveVersionsService archiveVersionsService,
            IArchiveVersionsSettings settings,
            IUserContextManager userContextManager,
            IUserAuthenticationService userAuthenticationService,
            IUserAuthorizationService userAuthorizationService,
            IJobExecutionObserver jobExecutionObserver)
            : base(userContextManager, userAuthenticationService, userAuthorizationService, jobExecutionObserver)
        {
            _archiveVersionsService = archiveVersionsService;
            _settings = settings;
            _transactionOptions = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.Zero };
        }

        protected override void ExecuteInternal(IJobExecutionContext context)
        {
            var archiveDate = DateTime.UtcNow - _settings.ArchiveVersionsInterval;
            using (Probe.Create("Archive"))
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, _transactionOptions))
            {
                _archiveVersionsService.Execute(archiveDate);
                transaction.Complete();
            }
        }
    }
}