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

        public static IList<HistoryEvent> Reopen(this IList<HistoryEvent> historyEvents, DataConverter dataConverter)
        {
            return historyEvents
                .Select(e => e is ExecutionCompletedEvent completedEvent
                    && completedEvent.OrchestrationStatus == OrchestrationStatus.Completed
                    ? e.Rewind(dataConverter) : e)
                .ToArray();
        }

        public static RewindResult Rewind(this IList<HistoryEvent> historyEvents, HistoryEvent rewindPoint, string reason, DataConverter dataConverter)
        {
            var runtimeState = new OrchestrationRuntimeState(historyEvents);

            var eventsToKeep = historyEvents.TakeWhile(e => e != rewindPoint).ToArray();
            var eventsToRewind = historyEvents.SkipWhile(e => e != rewindPoint).ToArray();

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
                            result.SubOrchestrationsInstancesToRewind.Add(subOrchestrationCreatedEvent.InstanceId);
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
                    var rewoundEvent = eventToRewind.Rewind(dataConverter);
                    result.HistoryEvents.Add(rewoundEvent);
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

        public static HistoryEvent Rewind(this HistoryEvent eventToRewind, DataConverter dataConverter)
        {
            var rewoundData = dataConverter.Serialize(eventToRewind);
            var genericEvent = new GenericEvent(eventToRewind.EventId, $"Rewound: {rewoundData}")
            {
                Timestamp = eventToRewind.Timestamp
            };
            return genericEvent;
        }
    }

    public class RewindResult
    {
        public List<TaskMessage> OutboundMessages { get; } = new List<TaskMessage>();
        public List<TaskMessage> TimerMessages { get; } = new List<TaskMessage>();
        public List<TaskMessage> OrchestratorMessages { get; } = new List<TaskMessage>();
        public List<string> SubOrchestrationsInstancesToRewind { get; } = new List<string>();
        public List<HistoryEvent> HistoryEvents { get; } = new List<HistoryEvent>();
        public OrchestrationRuntimeState NewRuntimeState { get; set; }
    }
}
