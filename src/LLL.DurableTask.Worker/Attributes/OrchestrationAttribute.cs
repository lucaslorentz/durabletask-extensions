using System;

namespace LLL.DurableTask.Worker.Attributes
{
    public class OrchestrationAttribute : Attribute
    {
        public OrchestrationAttribute(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }
    }
}
