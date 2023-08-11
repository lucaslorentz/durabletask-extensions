using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace LLL.DurableTask.Server.Api.Models;

public class CreateOrchestrationRequest
{
    [Required]
    public string Name { get; set; }

    public string Version { get; set; }

    public string InstanceId { get; set; }

    public JToken Input { get; set; }

    public Dictionary<string, string> Tags { get; set; }
}
