using System;
using DurableTask.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LLL.DurableTask.Server.Api.Models
{
    public class Orchestration
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }
        public DateTime? CompletedTime { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OrchestrationStatus Status { get; set; }

        public string CustomStatus { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
    }
}
