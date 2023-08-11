using System.Threading.Tasks;

namespace LLL.DurableTask.Core;

public interface IOrchestrationServiceRewindClient
{
    Task RewindTaskOrchestrationAsync(string instanceId, string reason);
}
