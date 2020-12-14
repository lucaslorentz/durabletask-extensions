using DurableTask.Core;

namespace LLL.DurableTask.Server.Grpc.Server
{
    class NameVersion : INameVersionInfo
    {
        public NameVersion()
        {
        }

        public NameVersion(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; set; }
        public string Version { get; set; }

        public override string ToString()
        {
            return $"{Name}_{Version}";
        }
    }
}
