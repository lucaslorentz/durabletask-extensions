using System;
using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Worker.Orchestrations;

public class ServiceProviderTaskOrchestration : TaskOrchestration
{
    public ServiceProviderTaskOrchestration(Func<IServiceProvider, TaskOrchestration> factory)
    {
        Factory = factory;
    }

    public Func<IServiceProvider, TaskOrchestration> Factory { get; }
    public TaskOrchestration Instance { get; private set; }

    public void Initialize(IServiceProvider serviceProvider)
    {
        if (Instance == null)
            Instance = Factory(serviceProvider);
    }

    public override Task<string> Execute(OrchestrationContext context, string input)
    {
        return Instance.Execute(context, input);
    }

    public override string GetStatus()
    {
        return Instance.GetStatus();
    }

    public override void RaiseEvent(OrchestrationContext context, string name, string input)
    {
        Instance.RaiseEvent(context, name, input);
    }
}
