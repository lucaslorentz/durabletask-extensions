using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore.Mappers
{
    public class InstanceMapper
    {
        private readonly EFCoreOrchestrationOptions _options;

        public InstanceMapper(IOptions<EFCoreOrchestrationOptions> options)
        {
            _options = options.Value;
        }

        public Instance CreateInstance(ExecutionStartedEvent executionStartedEvent)
        {
            var instance = new Instance
            {
                InstanceId = executionStartedEvent.OrchestrationInstance.InstanceId,
                LastExecutionId = executionStartedEvent.OrchestrationInstance.ExecutionId,
                LastQueueName = QueueMapper.ToQueueName(executionStartedEvent.Name, executionStartedEvent.Version)
            };
            return instance;
        }

        public void UpdateInstance(
            Instance instance,
            OrchestrationRuntimeState runtimeState)
        {
            instance.LastExecutionId = runtimeState.OrchestrationInstance.ExecutionId;
            instance.LastQueueName = QueueMapper.ToQueueName(runtimeState.Name, runtimeState.Version);
        }
    }
}
