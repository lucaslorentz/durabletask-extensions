using System;

namespace LLL.DurableTask.Worker.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class OrchestrationAttribute : Attribute
{
    public string Name { get; set; }
    public string Version { get; set; }
}
