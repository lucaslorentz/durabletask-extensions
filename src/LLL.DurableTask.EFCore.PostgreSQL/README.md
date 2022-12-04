# LLL.DurableTask.EFCore.PostgreSQL [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.EFCore.PostgreSQL)](https://www.nuget.org/packages/LLL.DurableTask.EFCore.PostgreSQL/)

Extension to EFCore storage with migrations and queries specific to PostgreSQL.

## Configuration

```C#
services.AddDurableTaskEFCoreStorage()
    .UseNpgsql("YOUR_CONNECTION_STRING");
```
