using System;
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
                LastQueue = QueueMapper.ToQueue(executionStartedEvent.Name, executionStartedEvent.Version),
                LockedUntil = DateTime.UtcNow
            };
            return instance;
        }

        public void UpdateInstance(
            Instance instance,
            OrchestrationRuntimeState runtimeState)
        {
            instance.LastExecutionId = runtimeState.OrchestrationInstance.ExecutionId;
            instance.LastQueue = QueueMapper.ToQueue(runtimeState.Name, runtimeState.Version);
        }
    }
}
