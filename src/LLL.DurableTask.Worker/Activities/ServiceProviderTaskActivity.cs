using System;
using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Worker.Activities;

public class ServiceProviderTaskActivity : TaskActivity
{
    public ServiceProviderTaskActivity(Func<IServiceProvider, TaskActivity> factory)
    {
        Factory = factory;
    }

    public Func<IServiceProvider, TaskActivity> Factory { get; }
    public TaskActivity Instance { get; private set; }

    public void Initialize(IServiceProvider serviceProvider)
    {
        if (Instance == null)
            Instance = Factory(serviceProvider);
    }

    public override string Run(TaskContext context, string input)
    {
        return Instance.Run(context, input);
    }

    public override async Task<string> RunAsync(TaskContext context, string input)
    {
        return await Instance.RunAsync(context, input);
    }
}
