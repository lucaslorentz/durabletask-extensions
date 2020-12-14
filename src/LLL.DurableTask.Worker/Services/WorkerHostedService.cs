using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using Microsoft.Extensions.Hosting;

namespace LLL.DurableTask.Worker.Services
{
    public class WorkerHostedService : IHostedService
    {
        private readonly IOrchestrationService _orchestrationService;
        private readonly TaskHubWorker _taskHubWorker;

        public WorkerHostedService(
            IOrchestrationService orchestrationService,
            TaskHubWorker taskHubWorker)
        {
            _orchestrationService = orchestrationService;
            _taskHubWorker = taskHubWorker;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _orchestrationService.CreateIfNotExistsAsync();

            await _taskHubWorker.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _taskHubWorker.StopAsync();
        }
    }
}
