using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Newtonsoft.Json.Linq;

namespace BpmnWorker.Activities;

[Activity(Name = "Script")]
public class ScriptActivity : ActivityBase<ScriptActivity.Input, object>
{
    private readonly IScriptExecutor _scriptExecutor;
    private readonly ILogger<ScriptActivity> _logger;

    public ScriptActivity(
        IScriptExecutor scriptExecutor,
        ILogger<ScriptActivity> logger)
    {
        _scriptExecutor = scriptExecutor;
        _logger = logger;
    }

    public override async Task<object> ExecuteAsync(Input input)
    {
        _logger.LogWarning("Executing script {format}", input.ScriptFormat);

        return await _scriptExecutor.Execute<JToken>(input.ScriptFormat, input.Script, input.Variables
            .ToObject<Dictionary<string, object>>());
    }

    public class Input
    {
        public string Name { get; set; }
        public string ScriptFormat { get; set; }
        public string Script { get; set; }
        public JObject Variables { get; set; }
    }
}
