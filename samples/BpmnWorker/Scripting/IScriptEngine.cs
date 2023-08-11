﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace BpmnWorker.Scripting;

public interface IScriptEngine
{
    Task<T> Execute<T>(string script, IDictionary<string, object> variables);
}
