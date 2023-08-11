using DurableTask.Core;

namespace LLL.DurableTask.EFCore.Mappers;

public static class QueueMapper
{
    public static string ToQueue(INameVersionInfo nameVersion)
    {
        return ToQueue(nameVersion.Name, nameVersion.Version);
    }

    public static string ToQueue(string name, string version)
    {
        if (string.IsNullOrEmpty(version))
            return name;

        return $"{name}_{version}";
    }
}
