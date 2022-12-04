# LLL.DurableTask.EFCore.MySql [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.EFCore.MySql)](https://www.nuget.org/packages/LLL.DurableTask.EFCore.MySql/)

Extension to EFCore storage with migrations and queries specific to MySql.

## Configuration

```C#
services.AddDurableTaskEFCoreStorage()
    .UseMySql(ServerVersion.AutoDetect("YOUR_CONNECTION_STRING"));
```
