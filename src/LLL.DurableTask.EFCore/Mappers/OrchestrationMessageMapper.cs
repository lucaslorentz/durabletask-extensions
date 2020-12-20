using System;
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

        public OrchestrationMessage CreateOrchestratorMessage(
            TaskMessage message,
            int sequence)
        {
            return new OrchestrationMessage
            {
                Id = Guid.NewGuid(),
                InstanceId = message.OrchestrationInstance.InstanceId,
                ExecutionId = message.OrchestrationInstance.ExecutionId,
                SequenceNumber = sequence,
                AvailableAt = message.Event is TimerFiredEvent timerFiredEvent
                    ? timerFiredEvent.FireAt
                    : DateTime.UtcNow,
                Message = _options.DataConverter.Serialize(message),
            };
        }
    }
}
