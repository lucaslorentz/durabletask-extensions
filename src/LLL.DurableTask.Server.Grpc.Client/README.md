# LLL.DurableTask.Server.Grpc.Client [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.Server.Grpc.Client)](https://www.nuget.org/packages/LLL.DurableTask.Server.Grpc.Client/)

Durable Task storage implementation using server GRPC endpoints.

Supports same features as the storage configured in the server.

## Configuration

```C#
services.AddDurableTaskServerGrpcStorage(options =>
{
    options.BaseAddress = new Uri("YOUR_SERVER_ADDRESS");
});
```
