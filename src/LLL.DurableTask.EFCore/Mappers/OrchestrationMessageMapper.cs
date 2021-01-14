using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore.Mappers
{
    public class OrchestrationMessageMapper
    {
        private readonly EFCoreOrchestrationOptions _options;

        public OrchestrationMessageMapper(IOptions<EFCoreOrchestrationOptions> options)
        {
            _options = options.Value;
        }

        public async Task<OrchestrationMessage> CreateOrchestrationMessageAsync(
            TaskMessage message,
            int sequence,
            OrchestrationDbContext context,
            IDictionary<string, string> knownQueues = null)
        {
            if (knownQueues == null || !knownQueues.TryGetValue(message.OrchestrationInstance.InstanceId, out var queue))
            {
                var instance = await context.Instances.FindAsync(message.OrchestrationInstance.InstanceId);

                if (instance == null)
                    throw new Exception($"Instance {message.OrchestrationInstance.InstanceId} not found");

                queue = instance.LastQueueName;
            }

            return new OrchestrationMessage
            {
                Id = Guid.NewGuid(),
                InstanceId = message.OrchestrationInstance.InstanceId,
                ExecutionId = message.OrchestrationInstance.ExecutionId,
                Queue = queue,
                SequenceNumber = sequence,
                AvailableAt = message.Event is TimerFiredEvent timerFiredEvent
                    ? timerFiredEvent.FireAt.ToUniversalTime()
                    : DateTime.UtcNow,
                Message = _options.DataConverter.Serialize(message),
            };
        }
    }
}
