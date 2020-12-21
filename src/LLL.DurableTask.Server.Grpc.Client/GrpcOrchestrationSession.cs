using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using DurableTaskGrpc;
using LLL.DurableTask.Server.Grpc.Client.Internal;
using Microsoft.Extensions.Logging;
using TaskOrchestrationStream = Grpc.Core.AsyncDuplexStreamingCall<DurableTaskGrpc.TaskOrchestrationRequest, DurableTaskGrpc.TaskOrchestrationResponse>;
using TaskOrchestrationWorkItem = DurableTask.Core.TaskOrchestrationWorkItem;

namespace LLL.DurableTask.Server.Client
{
    public class GrpcOrchestrationSession : IOrchestrationSession
    {
        private static readonly TimeSpan _renewResponseTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan _completeResponseTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan _fetchResponseTimeout = TimeSpan.FromHours(1);
        private static readonly TimeSpan _releaseResponseTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan _abandonResponseTimeout = TimeSpan.FromSeconds(20);

        private readonly DataConverter _dataConverter = new GrpcJsonDataConverter();

        private readonly TaskOrchestrationStream _stream;
        private readonly ILogger _logger;

        public GrpcOrchestrationSession(
            TaskOrchestrationStream stream,
            ILogger logger)
        {
            _stream = stream;
            _logger = logger;
        }

        public async Task Renew(TaskOrchestrationWorkItem workItem)
        {
            var request = new TaskOrchestrationRequest
            {
                RenewRequest = new RenewTaskOrchestrationWorkItemLockRequest()
            };

            await _stream.RequestStream.WriteAsync(request);

            var cts = new CancellationTokenSource(_renewResponseTimeout);

            if (!await _stream.ResponseStream.MoveNext(cts.Token))
                throw new Exception("Session aborted");

            if (_stream.ResponseStream.Current.MessageCase != TaskOrchestrationResponse.MessageOneofCase.RenewResponse)
                throw new Exception("Unexpected response");

            var renewResponse = _stream.ResponseStream.Current.RenewResponse;
            workItem.LockedUntilUtc = renewResponse.LockedUntilUtc.ToDateTime();
        }

        public async Task Complete(
            TaskOrchestrationWorkItem workItem,
            OrchestrationRuntimeState newOrchestrationRuntimeState,
            IList<TaskMessage> outboundMessages,
            IList<TaskMessage> orchestratorMessages,
            IList<TaskMessage> timerMessages,
            TaskMessage continuedAsNewMessage,
            OrchestrationState orchestrationState)
        {
            var request = new TaskOrchestrationRequest
            {
                CompleteRequest = new CompleteTaskOrchestrationWorkItemRequest
                {
                    NewEvents = { newOrchestrationRuntimeState.NewEvents.Select(_dataConverter.Serialize) },
                    OutboundMessages = { outboundMessages.Select(_dataConverter.Serialize) },
                    OrchestratorMessages = { orchestratorMessages.Select(_dataConverter.Serialize) },
                    TimerMessages = { timerMessages.Select(_dataConverter.Serialize) },
                    ContinuedAsNewMessage = _dataConverter.Serialize(continuedAsNewMessage),
                    OrchestrationState = _dataConverter.Serialize(orchestrationState)
                }
            };

            await _stream.RequestStream.WriteAsync(request);

            var cts = new CancellationTokenSource(_completeResponseTimeout);

            if (!await _stream.ResponseStream.MoveNext(cts.Token))
                throw new Exception("Session aborted");

            if (_stream.ResponseStream.Current.MessageCase != TaskOrchestrationResponse.MessageOneofCase.CompleteResponse)
                throw new Exception("Unexpected response");
        }

        public async Task<IList<TaskMessage>> FetchNewOrchestrationMessagesAsync(
            TaskOrchestrationWorkItem workItem)
        {
            var request = new TaskOrchestrationRequest
            {
                FetchRequest = new FetchNewOrchestrationMessagesRequest()
            };

            await _stream.RequestStream.WriteAsync(request);

            var cts = new CancellationTokenSource(_fetchResponseTimeout);

            if (!await _stream.ResponseStream.MoveNext(cts.Token))
                throw new Exception("Session aborted");

            if (_stream.ResponseStream.Current.MessageCase != TaskOrchestrationResponse.MessageOneofCase.FetchResponse)
                throw new Exception("Unexpected response");

            var fetchResponse = _stream.ResponseStream.Current.FetchResponse;
            if (fetchResponse.NewMessages == null)
                return null;

            return fetchResponse.NewMessages.Messages
                .Select(x => _dataConverter.Deserialize<TaskMessage>(x))
                .ToArray();
        }

        public async Task Abandon(TaskOrchestrationWorkItem workItem)
        {
            var request = new TaskOrchestrationRequest
            {
                AbandonRequest = new AbandonTaskOrchestrationWorkItemLockRequest()
            };

            await _stream.RequestStream.WriteAsync(request);

            var cts = new CancellationTokenSource(_abandonResponseTimeout);

            if (!await _stream.ResponseStream.MoveNext(cts.Token))
                throw new Exception("Session aborted");

            if (_stream.ResponseStream.Current.MessageCase != TaskOrchestrationResponse.MessageOneofCase.AbandonResponse)
                throw new Exception("Unexpected response");
        }

        public async Task Release(TaskOrchestrationWorkItem workItem)
        {
            if (_stream == null)
                return;

            var request = new TaskOrchestrationRequest
            {
                ReleaseRequest = new ReleaseTaskOrchestrationWorkItemRequest()
            };

            await _stream.RequestStream.WriteAsync(request);

            var cts = new CancellationTokenSource(_releaseResponseTimeout);

            if (!await _stream.ResponseStream.MoveNext(cts.Token))
                throw new Exception("Session aborted");

            if (_stream.ResponseStream.Current.MessageCase != TaskOrchestrationResponse.MessageOneofCase.ReleaseResponse)
                throw new Exception("Unexpected response");

            await _stream.RequestStream.CompleteAsync();

            // Last read to close stream
            await _stream.ResponseStream.MoveNext(cts.Token);

            _stream.Dispose();
        }
    }
}
