using System;
using System.Collections.Generic;
using System.Linq;
using DurableTask.Core;
using DurableTask.Core.History;
using DurableTask.Core.Serializing;

namespace LLL.DurableTask.EFCore.Extensions
{
    public static class HistoryEventExtensions
    {
        public static int? GetTriggerEventId(this HistoryEvent historyEvent)
        {
            switch (historyEvent)
            {
                case TaskCompletedEvent taskCompletedEvent:
                    return taskCompletedEvent.TaskScheduledId;
                case TaskFailedEvent taskFailedEvent:
                    return taskFailedEvent.TaskScheduledId;
                case TimerFiredEvent timerFiredEvent:
                    return timerFiredEvent.TimerId;
                case SubOrchestrationInstanceCompletedEvent subOrchestrationCreated:
                    return subOrchestrationCreated.TaskScheduledId;
                case SubOrchestrationInstanceFailedEvent subOrchestrationFailed:
                    return subOrchestrationFailed.TaskScheduledId;
            }
            return null;
        }

        public static RewindResult Rewind(this IList<HistoryEvent> historyEvents, HistoryEvent rewindStart, string reason, DataConverter dataConverter)
        {
            var runtimeState = new OrchestrationRuntimeState(historyEvents);

            var eventsToKeep = historyEvents.TakeWhile(e => e != rewindStart).ToArray();
            var eventsToRewind = historyEvents.SkipWhile(e => e != rewindStart).ToArray();

            var result = new RewindResult();

            foreach (var eventToKeep in eventsToKeep)
            {
                result.HistoryEvents.Add(eventToKeep);

                var completionEventToRewind = eventsToRewind.FirstOrDefault(h => h.GetTriggerEventId() == eventToKeep.EventId);
                if (completionEventToRewind == null)
                    continue;

                switch (eventToKeep)
                {
                    case TaskScheduledEvent taskScheduledEvent:
                        {
                            result.OutboundMessages.Add(new TaskMessage
                            {
                                OrchestrationInstance = runtimeState.OrchestrationInstance,
                                Event = taskScheduledEvent,
                            });
                            break;
                        }
                    case TimerCreatedEvent timerCreatedEvent:
                        {
                            result.TimerMessages.Add(new TaskMessage
                            {
                                OrchestrationInstance = runtimeState.OrchestrationInstance,
                                Event = completionEventToRewind
                            });
                            break;
                        }
                    case SubOrchestrationInstanceCreatedEvent subOrchestrationCreatedEvent:
                        {
                            var subOrchestrationInstance = new OrchestrationInstance
                            {
                                InstanceId = Guid.NewGuid().ToString(),
                                ExecutionId = Guid.NewGuid().ToString()
                            };

                            var executionStartedEvent = new ExecutionStartedEvent(-1, subOrchestrationCreatedEvent.Input)
                            {
                                OrchestrationInstance = subOrchestrationInstance,
                                ParentInstance = new ParentInstance
                                {
                                    Name = runtimeState.Name,
                                    Version = runtimeState.Version,
                                    TaskScheduleId = subOrchestrationCreatedEvent.EventId,
                                    OrchestrationInstance = runtimeState.OrchestrationInstance
                                },
                                Name = subOrchestrationCreatedEvent.Name,
                                Version = subOrchestrationCreatedEvent.Version
                            };

                            result.OrchestratorMessages.Add(new TaskMessage
                            {
                                OrchestrationInstance = subOrchestrationInstance,
                                Event = executionStartedEvent
                            });
                            break;
                        }
                }
            }

            foreach (var eventToRewind in eventsToRewind)
            {
                if (eventToRewind is OrchestratorStartedEvent || eventToRewind is OrchestratorCompletedEvent)
                {
                    result.HistoryEvents.Add(eventToRewind);
                }
                else
                {
                    var rewoundData = dataConverter.Serialize(eventToRewind);
                    var genericEvent = new GenericEvent(eventToRewind.EventId, $"Rewound: {rewoundData}")
                    {
                        Timestamp = eventToRewind.Timestamp
                    };
                    result.HistoryEvents.Add(genericEvent);
                }
            }

            result.OrchestratorMessages.Add(new TaskMessage
            {
                OrchestrationInstance = runtimeState.OrchestrationInstance,
                Event = new GenericEvent(-1, $"Rewind reason: {reason}")
            });

            result.NewRuntimeState = new OrchestrationRuntimeState(result.HistoryEvents);

            return result;
        }
    }

    public class RewindResult
    {
        public List<TaskMessage> OutboundMessages { get; } = new List<TaskMessage>();
        public List<TaskMessage> TimerMessages { get; } = new List<TaskMessage>();
        public List<TaskMessage> OrchestratorMessages { get; } = new List<TaskMessage>();
        public List<HistoryEvent> HistoryEvents { get; } = new List<HistoryEvent>();
        public OrchestrationRuntimeState NewRuntimeState { get; set; }
    }
}
