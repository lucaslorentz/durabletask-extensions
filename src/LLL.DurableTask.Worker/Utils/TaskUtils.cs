using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LLL.DurableTask.Worker.Utils;

internal static class TaskUtils
{
    public static async Task<object> MaybeAwait(dynamic result, Type returnType)
    {
        var getAwaiterMethod = returnType.GetMethod(nameof(Task.GetAwaiter));
        if (getAwaiterMethod is null)
            return result;

        var getResultMethod = getAwaiterMethod.ReturnType.GetMethod(nameof(TaskAwaiter<object>.GetResult));
        if (getResultMethod is not null && getResultMethod.ReturnType != typeof(void))
        {
            return await result;
        }
        else
        {
            await result;
            return null;
        }
    }
}
