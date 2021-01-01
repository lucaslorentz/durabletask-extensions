using System;
using System.Collections.Generic;
using DurableTask.Core.History;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LLL.DurableTask.Core.Serializing
{
    public class HistoryEventConverter : JsonConverter
    {
        private static readonly Dictionary<EventType, Type> _typesMap = new Dictionary<EventType, Type>
        {
            { EventType.ContinueAsNew, typeof(ContinueAsNewEvent) },
            { EventType.EventRaised, typeof(EventRaisedEvent) },
            { EventType.EventSent, typeof(EventSentEvent) },
            { EventType.ExecutionCompleted, typeof(ExecutionCompletedEvent) },
            //{ EventType.ExecutionFailed, typeof(ExecutionFailedEvent) },
            { EventType.ExecutionStarted, typeof(ExecutionStartedEvent) },
            { EventType.ExecutionTerminated, typeof(ExecutionTerminatedEvent) },
            { EventType.GenericEvent, typeof(GenericEvent) },
            { EventType.HistoryState, typeof(HistoryStateEvent) },
            { EventType.OrchestratorCompleted, typeof(OrchestratorCompletedEvent) },
            { EventType.OrchestratorStarted, typeof(OrchestratorStartedEvent) },
            { EventType.SubOrchestrationInstanceCompleted, typeof(SubOrchestrationInstanceCompletedEvent) },
            { EventType.SubOrchestrationInstanceCreated, typeof(SubOrchestrationInstanceCreatedEvent) },
            { EventType.SubOrchestrationInstanceFailed, typeof(SubOrchestrationInstanceFailedEvent) },
            { EventType.TaskCompleted, typeof(TaskCompletedEvent) },
            { EventType.TaskFailed, typeof(TaskFailedEvent) },
            { EventType.TaskScheduled, typeof(TaskScheduledEvent) },
            { EventType.TimerCreated, typeof(TimerCreatedEvent) },
            { EventType.TimerFired, typeof(TimerFiredEvent) }
        };

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            // Avoid recursion by only converting when target type is unknown
            return objectType == typeof(HistoryEvent);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var eventTypeToken = jObject.GetValue("EventType", StringComparison.OrdinalIgnoreCase);

            if (eventTypeToken == null)
                throw new Exception("Expected EventType field in HistoryEvent");

            var eventType = eventTypeToken.ToObject<EventType>();

            var type = _typesMap[eventType];

            return jObject.ToObject(type, serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
