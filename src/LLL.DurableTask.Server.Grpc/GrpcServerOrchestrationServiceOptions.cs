using System;
using DurableTask.Core.Serializing;

namespace LLL.DurableTask.Server.Grpc
{
    public class GrpcServerOrchestrationServiceOptions
    {
        public DataConverter DataConverter { get; set; } = new JsonDataConverter();
    }
}
