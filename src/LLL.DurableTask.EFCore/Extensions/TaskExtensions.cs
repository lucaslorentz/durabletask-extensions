using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLL.DurableTask.EFCore.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<List<T>> WhenAllSerial<T>(this IEnumerable<Task<T>> tasks)
        {
            var result = new List<T>();
            foreach (var task in tasks)
            {
                result.Add(await task);
            }
            return result;
        }
    }
}
