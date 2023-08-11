using System.Threading.Tasks;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;

namespace LLL.DurableTask.Tests.Worker.TestHelpers;

[Orchestration(Name = "InvokeActivity")]
public class InvokeActivityOrchestration : OrchestrationBase<object, InvokeActivityOrchestration.Input>
{
    public class Input
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public object[] Parameters { get; set; } = System.Array.Empty<object>();
    }

    public override async Task<object> Execute(Input input)
    {
        return await Context.ScheduleTask<object>(input.Name, input.Version, input.Parameters);
    }
}
