using System;

namespace LLL.DurableTask.Worker.Attributes
{
    public class OrchestrationAttribute : Attribute
    {
        public string Name { get; }
        public string Version { get; }
    }
}
