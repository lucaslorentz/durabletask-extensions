using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using Microsoft.Extensions.Hosting;

namespace LLL.DurableTask.Server.Services;

public class ServerHostedService : IHostedService
{
    private readonly IOrchestrationService _orchestrationService;

    public ServerHostedService(IOrchestrationService orchestrationService)
    {
        _orchestrationService = orchestrationService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _orchestrationService.CreateIfNotExistsAsync();
        await _orchestrationService.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _orchestrationService.StopAsync();
    }
}
