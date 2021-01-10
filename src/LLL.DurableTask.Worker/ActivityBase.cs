using System;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker
{
    public abstract class ActivityBase<TInput, TResult> : AsyncTaskActivity<TInput, TResult>
    {
        public ActivityBase()
        {
            DataConverter = new TypelessJsonDataConverter();
        }

        protected override Task<TResult> ExecuteAsync(TaskContext context, TInput input)
        {
            return Task.FromResult(Execute(context, input));
        }

        protected virtual TResult Execute(TaskContext context, TInput input)
        {
            throw new NotImplementedException();
        }
    }
}
