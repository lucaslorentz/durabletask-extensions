using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker
{
    public abstract class DistributedAsyncTaskActivity<TInput, TResult> : AsyncTaskActivity<TInput, TResult>
    {
        public DistributedAsyncTaskActivity()
            : base(new UntypedJsonDataConverter())
        {
        }
    }

    public abstract class DistributedTaskActivity<TInput, TResult> : DistributedAsyncTaskActivity<TInput, TResult>
    {
        protected abstract TResult Execute(TaskContext context, TInput input);

        protected override Task<TResult> ExecuteAsync(TaskContext context, TInput input)
        {
            return Task.FromResult(Execute(context, input));
        }
    }
}
