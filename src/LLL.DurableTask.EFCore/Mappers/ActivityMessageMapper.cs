using System;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore.Mappers
{
    public class ActivityMessageMapper
    {
        private readonly EFCoreOrchestrationOptions _options;

        public ActivityMessageMapper(IOptions<EFCoreOrchestrationOptions> options)
        {
            _options = options.Value;
        }

        public ActivityMessage CreateActivityMessage(TaskMessage message)
        {
            var taskScheduledEvent = message.Event as TaskScheduledEvent;

            return new ActivityMessage
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Queue = QueueMapper.ToQueueName(taskScheduledEvent.Name, taskScheduledEvent.Version),
                Name = taskScheduledEvent.Name,
                Version = taskScheduledEvent.Version,
                InstanceId = message.OrchestrationInstance.InstanceId,
                ExecutionId = message.OrchestrationInstance.ExecutionId,
                Message = _options.DataConverter.Serialize(message),
                AvailableAt = DateTime.UtcNow
            };
        }
    }
}
