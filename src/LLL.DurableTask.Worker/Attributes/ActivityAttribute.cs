using System;

namespace LLL.DurableTask.Worker.Attributes
{
    public class ActivityAttribute : Attribute
    {
        public string Name { get; }
        public string Version { get; }
    }
}
