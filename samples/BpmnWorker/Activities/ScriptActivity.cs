using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BpmnWorker.Activities
{
    [Activity(Name = "Script")]
    public class ScriptActivity : DistributedAsyncTaskActivity<ScriptActivity.Input, object>
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

        protected override async Task<object> ExecuteAsync(TaskContext context, Input input)
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
}
