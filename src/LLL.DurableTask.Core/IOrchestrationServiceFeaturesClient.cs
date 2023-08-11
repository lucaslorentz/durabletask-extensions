using System.Threading.Tasks;

namespace LLL.DurableTask.Core;

public interface IOrchestrationServiceFeaturesClient
{
    Task<OrchestrationFeature[]> GetFeatures();
}
