# LLL.DurableTask.EFCore.InMemory [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.EFCore.InMemory)](https://www.nuget.org/packages/LLL.DurableTask.EFCore.InMemory/)

Extension to EFCore storage with queries specific to InMemory database.

## Configuration

```C#
services.AddDurableTaskEFCoreStorage()
    .UseInMemoryDatabase("DatabaseName");
```
