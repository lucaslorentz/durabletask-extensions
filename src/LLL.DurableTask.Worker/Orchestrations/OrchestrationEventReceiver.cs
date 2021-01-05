using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker.Orchestrations
{
    public class OrchestrationEventReceiver
    {
        private readonly DataConverter _dataConverter;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<TaskCompletionSource<string>>> _waitingQueues;

        public OrchestrationEventReceiver(DataConverter dataConverter)
        {
            _dataConverter = dataConverter;
            _waitingQueues = new ConcurrentDictionary<string, ConcurrentQueue<TaskCompletionSource<string>>>();
        }

        public async Task<T> WaitForEventAsync<T>(string eventType, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();

            var waitingQueue = _waitingQueues.GetOrAdd(eventType, _ => new ConcurrentQueue<TaskCompletionSource<string>>());

            waitingQueue.Enqueue(taskCompletionSource);

            using (var registration = cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken)))
            {
                var input = await taskCompletionSource.Task;
                return _dataConverter.Deserialize<T>(input);
            }
        }

        public void RaiseEvent(string eventType, string input)
        {
            if (_waitingQueues.TryRemove(eventType, out var queue))
            {
                while (queue.TryDequeue(out var waitingTask))
                    waitingTask.TrySetResult(input);
            }
        }
    }
}
