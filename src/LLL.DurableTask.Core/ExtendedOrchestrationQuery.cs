using DurableTask.Core.Query;

namespace LLL.DurableTask.Core
{
    public class ExtendedOrchestrationQuery : OrchestrationQuery
    {
        public string NamePrefix { get; set; }
        public bool IncludePreviousExecutions { get; set; }
    }
}
