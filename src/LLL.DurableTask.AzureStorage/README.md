# LLL.DurableTask.AzureStorage [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.AzureStorage)](https://www.nuget.org/packages/LLL.DurableTask.AzureStorage/)

Dependency injection extensions to configure the official Azure Storage.

## Configuration

```C#
services.AddDurableTaskAzureStorage(options =>
{
    options.TaskHubName = "Test";
    options.StorageConnectionString = "UseDevelopmentStorage=true";
});
```
