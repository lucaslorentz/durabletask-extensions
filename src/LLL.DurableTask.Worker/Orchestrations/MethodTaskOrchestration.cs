using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker.Orchestrations;

public class MethodTaskOrchestration : TaskOrchestration
{
    private readonly MethodInfo _methodInfo;
    private readonly DataConverter _dataConverter;
    private ExtendedOrchestrationContext _extendedContext;

    public MethodTaskOrchestration(
        object instance,
        MethodInfo methodInfo)
    {
        Instance = instance;
        _methodInfo = methodInfo;
        _dataConverter = new TypelessJsonDataConverter();
    }

    public object Instance { get; }

    public override async Task<string> Execute(OrchestrationContext context, string input)
    {
        context.MessageDataConverter = new TypelessJsonDataConverter();
        context.ErrorDataConverter = new TypelessJsonDataConverter();

        var parameters = PrepareParameters(input, new Dictionary<Type, Func<object>>
        {
            [typeof(OrchestrationContext)] = () => context,
            [typeof(ExtendedOrchestrationContext)] = () => _extendedContext = new ExtendedOrchestrationContext(context),
        });

        try
        {
            var result = _methodInfo.Invoke(Instance, parameters);
            return await SerializeResult(result);
        }
        catch (TargetInvocationException ex)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    public override string GetStatus()
    {
        return _extendedContext?.StatusProvider?.Invoke();
    }

    public override void RaiseEvent(OrchestrationContext context, string name, string input)
    {
        _extendedContext?.RaiseEvent(name, input);
    }

    private object[] PrepareParameters(
        string input,
        Dictionary<Type, Func<object>> factories)
    {
        var parameters = new List<object>();

        foreach (var p in _methodInfo.GetParameters())
        {
            object value;

            if (factories.TryGetValue(p.ParameterType, out var factory))
            {
                value = factory();
            }
            else if (input is not null)
            {
                value = _dataConverter.Deserialize(input, p.ParameterType);
            }
            else if (p.HasDefaultValue)
            {
                value = p.DefaultValue;
            }
            else
                throw new Exception($"Orchestration input was not provided.");

            parameters.Add(value);
        }

        return parameters.ToArray();
    }

    private async Task<string> SerializeResult(object result)
    {
        if (result is Task task)
        {
            if (_methodInfo.ReturnType.IsGenericType)
            {
                result = await (dynamic)task;
            }
            else
            {
                await task;
                result = null;
            }
        }

        var serializedResult = _dataConverter.Serialize(result);

        return serializedResult;
    }
}
