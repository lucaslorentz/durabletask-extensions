namespace BpmnWorker.Scripting;

public interface IScriptEngineFactory
{
    string Language { get; }

    IScriptEngine Create();
}
