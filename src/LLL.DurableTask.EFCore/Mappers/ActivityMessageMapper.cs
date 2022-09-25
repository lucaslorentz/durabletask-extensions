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

        public ActivityMessage CreateActivityMessage(
            TaskMessage message,
            string replyQueue)
        {
            var taskScheduledEvent = message.Event as TaskScheduledEvent;

            return new ActivityMessage
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Queue = QueueMapper.ToQueue(taskScheduledEvent.Name, taskScheduledEvent.Version),
                ReplyQueue = replyQueue,
                InstanceId = message.OrchestrationInstance.InstanceId,
                Message = _options.DataConverter.Serialize(message),
                LockedUntil = DateTime.UtcNow
            };
        }
    }
}
