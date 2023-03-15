# LLL.DurableTask.Worker [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.Worker)](https://www.nuget.org/packages/LLL.DurableTask.Worker/)

Dependency injection extensions to configure TaskHubWorker.

Allows execution of orchestration/activity tasks.

A service scope is created for each orchestration and activity execution.

Orchestrations/activities/middlewares supports dependency injection.

## Depends on

- Storage

## Configuration

```C#
services.AddDurableTaskWorker(builder =>
{
    // Add orchestration with default name and version
    builder.AddOrchestration<BookParallel>();

    // Add orchestration with specific name and version
    builder.AddOrchestration<BookParallel>("BookParallel", "v1");

    // Add activity with default name and version
    builder.AddActivity<BookHotelActivity>();

    // Add activity with specific name and version
    builder.AddActivity<BookHotelActivity>("BookHotel", "v1");
});
```

Or you can also scan an assembly to add all orchestrations and/or activities annotated with attributes [OrchestrationAttribute](src/LLL.DurableTask.Worker/Attributes/OrchestrationAttribute.cs) or [ActivityAttribute](src/LLL.DurableTask.Worker/Attributes/ActivityAttribute.cs):

```C#
services.AddDurableTaskWorker(builder =>
{
    // Adds all orchestrations and activities from assembly
    builder.AddAnnotatedFromAssembly(typeof(Startup).Assembly);

    // Add only orchestrations from assembly
    builder.AddAnnotatedOrchestrationsFromAssembly(typeof(Startup).Assembly);

    // Add only activities from assembly
    builder.AddAnnotatedActivitiesFromAssembly(typeof(Startup).Assembly);
});
```

Registering activities from interfaces:
```C#
services.AddDurableTaskWorker(builder =>
{
    // Add all interface methods as activities
    builder.AddActivitiesFromInterface<IMyActivities, MyActivities>();
});
```

**NOTE:** When using storages that doesn't support distributed workers, make sure all your orchestrations and activities are implemented in the same worker and add the following lines to your worker configuration:

```C#
services.AddDurableTaskWorker(builder =>
{
    ...
    builder.HasAllOrchestrations = true;
    builder.HasAllActivities = true;
});
```