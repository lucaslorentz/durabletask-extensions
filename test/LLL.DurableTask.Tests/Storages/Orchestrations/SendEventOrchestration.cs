using System.Threading.Tasks;
using DurableTask.Core;

namespace LLL.DurableTask.Tests.Storage.Orchestrations;

public class SendEventOrchestration : TaskOrchestration<object, SendEventOrchestration.Input>
{
    public class Input
    {
        public string TargetInstanceId { get; set; }
        public string EventName { get; set; }
        public object EventInput { get; set; }
    }

    public const string Name = "SendEvent";
    public const string Version = "v1";

    public override Task<object> RunTask(OrchestrationContext context, Input input)
    {
        var orchestrationInstance = new OrchestrationInstance
        {
            InstanceId = input.TargetInstanceId
        };

        context.SendEvent(orchestrationInstance, input.EventName, input.EventInput);

        return Task.FromResult(default(object));
    }
}
