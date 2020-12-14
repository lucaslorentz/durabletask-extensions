using DurableTask.Core;

namespace LLL.DurableTask.EFCore.Mappers
{
    public static class QueueMapper
    {
        public static string ToQueueName(INameVersionInfo nameVersion)
        {
            return ToQueueName(nameVersion.Name, nameVersion.Version);
        }

        public static string ToQueueName(string name, string version)
        {
            if (string.IsNullOrEmpty(version))
                return name;

            return $"{name}_{version}";
        }
    }
}
