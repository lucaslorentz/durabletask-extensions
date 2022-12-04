# LLL.DurableTask.Server.Grpc [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.Server.Grpc)](https://www.nuget.org/packages/LLL.DurableTask.Server.Grpc/)

GRPC endpoints for server.

The chatty orchestration execution communication is done with bidirectional streaming, maintaining the orchestration session alive in the server side.

Activity execution and all remaining communication is done with non streamed rpc.

## Configuration

```C#
services.AddDurableTaskServer(builder =>
{
    builder.AddGrpcEndpoints();
});
...
app.UseEndpoints(endpoints =>
{
    endpoints.MapDurableTaskServerGrpcService();
});
```
