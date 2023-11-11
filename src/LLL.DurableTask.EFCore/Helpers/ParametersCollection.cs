using System.Collections;
using System.Collections.Generic;

namespace LLL.DurableTask.EFCore;

public class ParametersCollection : IEnumerable<object>
{
    private readonly List<object> _values = new();

    public string Add(object value)
    {
        _values.Add(value);
        return $"{{{_values.Count - 1}}}";
    }

    public object[] ToArray()
    {
        return _values.ToArray();
    }

    public IEnumerator<object> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _values.GetEnumerator();
    }
}