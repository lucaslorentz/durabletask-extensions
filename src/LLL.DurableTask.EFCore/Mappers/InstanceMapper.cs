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
                AvailableAt = DateTime.UtcNow,
                Queue = QueueMapper.ToQueueName(executionStartedEvent.Name, executionStartedEvent.Version)
            };
            return instance;
        }

        public void UpdateInstance(
            Instance instance,
            OrchestrationState orchestrationState)
        {
            instance.LastExecutionId = orchestrationState.OrchestrationInstance.ExecutionId;
            instance.Queue = QueueMapper.ToQueueName(orchestrationState.Name, orchestrationState.Version);
        }
    }
}
