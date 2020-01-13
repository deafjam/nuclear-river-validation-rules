using NuClear.Jobs;
using NuClear.Security.API.Auth;
using NuClear.Security.API.Context;
using Quartz;
using System.Threading.Tasks;

namespace NuClear.ValidationRules.Replication.Host.Jobs
{
    public sealed class DebugTestJob : TaskServiceJobBase
    {
        public DebugTestJob(IUserContextManager userContextManager,
                            IUserAuthenticationService userAuthenticationService,
                            IUserAuthorizationService userAuthorizationService,
                            IJobExecutionObserver jobExecutionObserver)
            : base(userContextManager, userAuthenticationService, userAuthorizationService, jobExecutionObserver)
        {
        }

        protected override void ExecuteInternal(IJobExecutionContext context)
        {
            Task.Delay(5000);
        }
    }
}