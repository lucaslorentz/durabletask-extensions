using System;
using DurableTask.Core;

namespace LLL.DurableTask.Worker.ObjectCreators;

public class FactoryObjectCreator<T> : ObjectCreator<T>
{
    private readonly Func<T> _factory;

    public FactoryObjectCreator(
        string name,
        string version,
        Func<T> factory)
    {
        Name = name;
        Version = version;
        _factory = factory;
    }

    public override T Create()
    {
        return _factory();
    }
}
