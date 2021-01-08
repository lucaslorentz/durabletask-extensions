using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Core.Serializing;

namespace LLL.DurableTask.Worker.Utils
{
    public class OrchestrationEventReceiver
    {
        private readonly DataConverter _dataConverter;
        private readonly ConcurrentDictionary<string, Action<string>> _callbacks;

        public OrchestrationEventReceiver()
            : this(new TypelessJsonDataConverter())
        {
        }

        public OrchestrationEventReceiver(DataConverter dataConverter)
        {
            _dataConverter = dataConverter;
            _callbacks = new ConcurrentDictionary<string, Action<string>>();
        }

        public async Task<T> WaitForEventAsync<T>(string eventType, CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();

            _callbacks.AddOrUpdate(eventType,
                _ => Callback,
                (_, existingCallback) => existingCallback + Callback);

            using (var registration = cancellationToken.Register(Cancel))
            {
                var input = await taskCompletionSource.Task;
                return _dataConverter.Deserialize<T>(input);
            }

            void Callback(string input)
            {
                taskCompletionSource.TrySetResult(input);
            }
            void Cancel()
            {
                _callbacks.AddOrUpdate(eventType,
                    _ => null,
                    (_, existingCallback) => existingCallback - Callback);

                taskCompletionSource.TrySetCanceled(cancellationToken);
            }
        }

        public void RaiseEvent(string eventType, string input)
        {
            if (_callbacks.TryRemove(eventType, out var callback))
            {
                callback(input);
            }
        }
    }
}
