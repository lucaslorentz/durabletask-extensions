using BpmnWorker.Scripting;

namespace BpmnWorker.Activities;

public class ScriptExecutor : IScriptExecutor
{
    private readonly ILookup<string, IScriptEngineFactory> _scriptEngineFactories;

    public ScriptExecutor(IEnumerable<IScriptEngineFactory> scriptEngineFactories)
    {
        _scriptEngineFactories = scriptEngineFactories
            .ToLookup(f => f.Language, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<T> Execute<T>(string language, string script, IDictionary<string, object> variables = null)
    {
        var scriptEngineFactory = _scriptEngineFactories[language].FirstOrDefault()
            ?? throw new NotSupportedException($"Script language {language} is not supported");
        var scriptEngine = scriptEngineFactory.Create();

        return await scriptEngine.Execute<T>(script, variables);
    }
}
