using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;

namespace BpmnWorker.Scripting;

public class JavaScriptScriptEngine : IScriptEngine
{
    public Task<T> Execute<T>(string script, IDictionary<string, object> variables)
    {
        using var engine = new V8ScriptEngine();
        if (variables != null)
        {
            foreach (var kv in variables)
            {
                if (kv.Value != null)
                {
                    var inputJson = JsonConvert.SerializeObject(kv.Value);
                    var inputJs = engine.Script.JSON.parse(inputJson);
                    engine.Script[kv.Key] = inputJs;
                }
            }
        }

        engine.AddHostType("Console", typeof(Console));

        var outputJs = engine.Evaluate(script);
        if (outputJs == null)
            return Task.FromResult(default(T));

        if (engine.Script.JSON.stringify(outputJs) is not string outputJson)
            return Task.FromResult(default(T));

        var output = JsonConvert.DeserializeObject<T>(outputJson);
        return Task.FromResult(output);
    }
}
