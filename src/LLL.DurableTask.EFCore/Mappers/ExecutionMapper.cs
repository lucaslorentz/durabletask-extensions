using System;
using System.Collections.Generic;
using System.Linq;
using DurableTask.Core;
using DurableTask.Core.History;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.Extensions.Options;

namespace LLL.DurableTask.EFCore.Mappers
{
    public class ExecutionMapper
    {
        private readonly EFCoreOrchestrationOptions _options;

        public ExecutionMapper(IOptions<EFCoreOrchestrationOptions> options)
        {
            _options = options.Value;
        }

        public Execution CreateExecution(OrchestrationRuntimeState runtimeState)
        {
            var execution = new Execution();
            UpdateExecution(execution, runtimeState);
            return execution;
        }

        public void UpdateExecution(
            Execution execution,
            OrchestrationRuntimeState runtimeState)
        {
            execution.InstanceId = runtimeState.OrchestrationInstance.InstanceId;
            execution.ExecutionId = runtimeState.OrchestrationInstance.ExecutionId;
            execution.Name = runtimeState.Name;
            execution.Version = runtimeState.Version;
            execution.Status = runtimeState.OrchestrationStatus;
            execution.FailureDetails = runtimeState.FailureDetails != null
                ? _options.DataConverter.Serialize(runtimeState.FailureDetails)
                : null;
            execution.CreatedTime = runtimeState.CreatedTime;
            execution.CompletedTime = runtimeState.CompletedTime;
            execution.LastUpdatedTime = DateTime.UtcNow;
            execution.CompressedSize = runtimeState.CompressedSize;
            execution.Size = runtimeState.Size;
            execution.Input = runtimeState.Input;
            execution.Output = runtimeState.Output;
            execution.CustomStatus = runtimeState.Status;
            execution.ParentInstance = runtimeState.ParentInstance != null
                ? _options.DataConverter.Serialize(runtimeState.ParentInstance)
                : null;

            var newTags = new HashSet<Tag>();
            if (runtimeState.Tags != null)
            {
                var existingTags = execution.Tags.ToLookup(t => t.Name);
                foreach (var keyValue in runtimeState.Tags)
                {
                    var tag = existingTags[keyValue.Key].FirstOrDefault() ?? new Tag();
                    tag.Name = keyValue.Key;
                    tag.Value = keyValue.Value;
                    newTags.Add(tag);
                }
            }
            execution.Tags.RemoveWhere(t => !newTags.Contains(t));
            execution.Tags.UnionWith(newTags);
        }

        public Event CreateEvent(OrchestrationInstance orchestrationInstance, int sequenceNumber, HistoryEvent historyEvent)
        {
            var @event = new Event
            {
                Id = Guid.NewGuid(),
                InstanceId = orchestrationInstance.InstanceId,
                ExecutionId = orchestrationInstance.ExecutionId,
                SequenceNumber = sequenceNumber
            };
            UpdateEvent(@event, historyEvent);
            return @event;
        }

        public void UpdateEvent(Event @event, HistoryEvent historyEvent)
        {
            @event.Content = _options.DataConverter.Serialize(historyEvent);
        }

        public OrchestrationState MapToState(Execution execution)
        {
            var orchestrationState = new OrchestrationState();
            orchestrationState.OrchestrationInstance = new OrchestrationInstance
            {
                InstanceId = execution.InstanceId,
                ExecutionId = execution.ExecutionId
            };
            orchestrationState.Name = execution.Name;
            orchestrationState.Version = execution.Version;
            orchestrationState.OrchestrationStatus = execution.Status;
            orchestrationState.FailureDetails = !string.IsNullOrEmpty(execution.FailureDetails)
                ? _options.DataConverter.Deserialize<FailureDetails>(execution.FailureDetails)
                : null;
            orchestrationState.CreatedTime = execution.CreatedTime;
            orchestrationState.LastUpdatedTime = execution.LastUpdatedTime;
            orchestrationState.CompletedTime = execution.CompletedTime;
            orchestrationState.CompressedSize = execution.CompressedSize;
            orchestrationState.Size = execution.Size;
            orchestrationState.Input = execution.Input;
            orchestrationState.Output = execution.Output;
            orchestrationState.Status = execution.CustomStatus;
            orchestrationState.ParentInstance = !string.IsNullOrEmpty(execution.ParentInstance)
                ? _options.DataConverter.Deserialize<ParentInstance>(execution.ParentInstance)
                : null;
            orchestrationState.Tags = execution.Tags.ToDictionary(t => t.Name, t => t.Value);
            return orchestrationState;
        }
    }
}
