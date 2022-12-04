# LLL.DurableTask.Client [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.Client)](https://www.nuget.org/packages/LLL.DurableTask.Client/)

Dependency injection extensions to configure TaskHubClient.

Allows management of orchestrations via code.

## Depends on

- Storage

## Configuration

```C#
services.AddDurableTaskClient();
```

## Usage

```C#
public IActionResult BookPackage([FromService] TaskHubClient taskHubClient) {
    await taskHubClient.CreateOrchestrationInstanceAsync("BookParallel", "v1", new {
        bookFlight: true,
        bookHotel: true,
        bookCar: true
    });
    ...
}
```
