using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker
{
    public abstract class ActivityBase<TInput, TResult> : AsyncTaskActivity<TInput, TResult>
    {
        public TaskContext Context { get; private set; }

        public ActivityBase()
        {
            DataConverter = new TypelessJsonDataConverter();
        }

        protected sealed override async Task<TResult> ExecuteAsync(TaskContext context, TInput input)
        {
            Context = context;
            return await ExecuteAsync(input);
        }

        public abstract Task<TResult> ExecuteAsync(TInput input);
    }
}
