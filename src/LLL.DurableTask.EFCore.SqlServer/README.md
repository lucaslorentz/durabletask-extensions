# LLL.DurableTask.EFCore.SqlServer [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.EFCore.SqlServer)](https://www.nuget.org/packages/LLL.DurableTask.EFCore.SqlServer/)

Extension to EFCore storage with migrations and queries specific to Sql Server.

## Configuration

```C#
services.AddDurableTaskEFCoreStorage()
    .UseSqlServer("YOUR_CONNECTION_STRING");
```
