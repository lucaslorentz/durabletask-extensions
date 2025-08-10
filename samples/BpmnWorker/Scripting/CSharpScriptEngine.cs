using Microsoft.CodeAnalysis.CSharp.Scripting;
using Newtonsoft.Json;

namespace BpmnWorker.Scripting;

public class CSharpScriptEngine : IScriptEngine
{
    public async Task<T> Execute<T>(string script, IDictionary<string, object> variables)
    {
        var output = await CSharpScript.EvaluateAsync(script);
        if (output is null)
            return default;

        if (output is T t)
            return t;

        var serialized = JsonConvert.SerializeObject(output);
        return JsonConvert.DeserializeObject<T>(serialized);
    }
}
