using System.Collections.Generic;
using System.Threading.Tasks;

namespace BpmnWorker.Activities
{
    public interface IScriptExecutor
    {
        Task<T> Execute<T>(string language, string script, IDictionary<string, object> variables = null);
    }
}