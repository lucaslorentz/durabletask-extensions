using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace BpmnWorker.Scripting
{
    public class CSharpScriptEngine : IScriptEngine
    {
        public async Task<T> Execute<T>(string script, IDictionary<string, object> variables)
        {
            var output = await CSharpScript.EvaluateAsync(script);
            //return output != null ? JToken.FromObject(output) : null;
            return default(T);
        }
    }
}
