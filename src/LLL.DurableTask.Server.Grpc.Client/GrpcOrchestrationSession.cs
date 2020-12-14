using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using LLL.DurableTask.Server.Grpc.Client.Internal;
using DurableTaskHub;
using Microsoft.Extensions.Logging;
using HubGRPCOrchestrationStream = Grpc.Core.AsyncDuplexStreamingCall<DurableTaskHub.TaskOrchestrationRequest, DurableTaskHub.TaskOrchestrationResponse>;

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

        private readonly HubGRPCOrchestrationStream _stream;
        private readonly ILogger _logger;

        public GrpcOrchestrationSession(
            HubGRPCOrchestrationStream stream,
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
                throw new Exception();

            var renewResponse = _stream.ResponseStream.Current.RenewResponse;
            if (renewResponse == null)
                throw new Exception("Unexpected response");

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

            await _stream.ResponseStream.MoveNext(cts.Token);
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

            if (_stream.ResponseStream.Current.FetchResponseIsNull)
                return null;

            var fetchResponse = _stream.ResponseStream.Current.FetchResponse;
            if (fetchResponse == null)
                throw new Exception("Didn't receive fetch response");

            return fetchResponse.NewMessages
                .Select(x => _dataConverter.Deserialize<TaskMessage>(x))
                .ToArray();
        }

        public async Task Release(TaskOrchestrationWorkItem workItem)
        {
            var request = new TaskOrchestrationRequest
            {
                ReleaseRequest = new ReleaseTaskOrchestrationWorkItemRequest()
            };

            await _stream.RequestStream.WriteAsync(request);

            var cts = new CancellationTokenSource(_releaseResponseTimeout);

            await _stream.ResponseStream.MoveNext(cts.Token);

            _stream.Dispose();
        }

        public async Task Abandon(TaskOrchestrationWorkItem workItem)
        {
            var request = new TaskOrchestrationRequest
            {
                AbandonRequest = new AbandonTaskOrchestrationWorkItemLockRequest()
            };

            await _stream.RequestStream.WriteAsync(request);

            var cts = new CancellationTokenSource(_abandonResponseTimeout);

            await _stream.ResponseStream.MoveNext(cts.Token);
        }
    }
}
