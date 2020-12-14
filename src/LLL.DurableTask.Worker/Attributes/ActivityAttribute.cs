using System;

namespace LLL.DurableTask.Worker.Attributes
{
    public class ActivityAttribute : Attribute
    {
        public ActivityAttribute(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }
    }
}
