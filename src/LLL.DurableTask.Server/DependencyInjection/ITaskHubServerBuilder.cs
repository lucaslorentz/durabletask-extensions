using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.Server.Configuration
{
    public interface ITaskHubServerBuilder
    {
        IServiceCollection Services { get; }
    }
}
