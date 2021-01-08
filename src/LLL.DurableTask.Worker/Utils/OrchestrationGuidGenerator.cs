using System;

namespace LLL.DurableTask.Worker.Utils
{
    public class OrchestrationGuidGenerator
    {
        private static readonly Guid _orchestrationGuidNamespace = new Guid("93810b2d-3411-4fc0-b51b-47f2233dac7a");

        private int _count = 0;

        public string ExecutionId { get; }

        public OrchestrationGuidGenerator(string executionId)
        {
            ExecutionId = executionId;
        }

        public Guid NewGuid()
        {
            return DeterministicGuid.Create(_orchestrationGuidNamespace, $"{ExecutionId}/{++_count}");
        }
    }
}
