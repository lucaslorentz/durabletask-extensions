using System;
using Microsoft.Extensions.DependencyInjection;

namespace BpmnWorker.Scripting;

public class ServiceProviderScriptEngineFactory<T> : ResolveTypeScriptEngineFactory
{
    public ServiceProviderScriptEngineFactory(IServiceProvider serviceProvider,
        string language)
        : base(serviceProvider, language, typeof(T))
    {
    }
}

public class ResolveTypeScriptEngineFactory : IScriptEngineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Type _type;

    public ResolveTypeScriptEngineFactory(
        IServiceProvider serviceProvider,
        string language,
        Type type)
    {
        _serviceProvider = serviceProvider;
        Language = language;
        _type = type;
    }

    public string Language { get; }

    public IScriptEngine Create()
    {
        return _serviceProvider.GetRequiredService(_type) as IScriptEngine;
    }
}
