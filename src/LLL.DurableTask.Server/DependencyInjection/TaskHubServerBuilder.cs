using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.Server.Configuration
{
    public class TaskHubServerBuilder : ITaskHubServerBuilder
    {
        public IServiceCollection Services { get; }

        public TaskHubServerBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
