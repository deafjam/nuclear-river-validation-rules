using System.Threading.Tasks;

namespace NuClear.StateInitialization.Core.Commands
{
    public class ExecutionMode
    {
        private const int DefaultDegreeOfParallelizm = -1;
        public static ExecutionMode Sequential = new ExecutionMode(1, false);
        public static ExecutionMode Parallel = new ExecutionMode(DefaultDegreeOfParallelizm, false);
        public static ExecutionMode ShadowParallel = new ExecutionMode(DefaultDegreeOfParallelizm, true);

        private readonly int _maxDegreeOfParallelizm;

        public ExecutionMode(int maxDegreeOfParallelizm, bool shadow)
        {
            _maxDegreeOfParallelizm = maxDegreeOfParallelizm;
            Shadow = shadow;
        }

        public bool Shadow { get; }

        public virtual ParallelOptions ParallelOptions
            => _maxDegreeOfParallelizm == DefaultDegreeOfParallelizm
                   ? new ParallelOptions()
                   : new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelizm };
    }
}