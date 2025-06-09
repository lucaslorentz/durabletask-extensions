using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.Tests.Storage.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages;

public abstract class EFCoreTestBase : StorageTestBase
{
    private readonly ITestOutputHelper _output;

    protected EFCoreTestBase(ITestOutputHelper output) : base(output)
    {
        _output = output;
    }

    [SkippableFact]
    public async Task TryLockNextInstanceAsync()
    {
        var taskHubClient = _host.Services.GetService<TaskHubClient>();

        await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
            DateTime.UtcNow,
            OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);

        for (var i = 1; i <= 4; i++)
            await taskHubClient.CreateOrchestrationInstanceAsync($"o{i}", "", $"i{i}", null);

        var dbContextFactory = _host.Services.GetService<IDbContextFactory<OrchestrationDbContext>>();
        var dbContextExtensions = _host.Services.GetService<OrchestrationDbContextExtensions>();

        using var dbContext1 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext2 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext3 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext4 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext5 = await dbContextFactory.CreateDbContextAsync();

        var lockTimeout = TimeSpan.FromMinutes(1);

        await dbContextExtensions.WithinTransaction(dbContext1, async () =>
        {
            var instance1 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext1, lockTimeout);
            instance1.Should().NotBeNull();
            instance1.InstanceId.Should().Be("i1");

            await dbContextExtensions.WithinTransaction(dbContext2, async () =>
            {
                var instance2 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext2, lockTimeout);
                instance2.Should().NotBeNull();
                instance2.InstanceId.Should().Be("i2");

                await dbContextExtensions.WithinTransaction(dbContext3, async () =>
                {
                    var instance3 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext3, lockTimeout);
                    instance3.Should().NotBeNull();
                    instance3.InstanceId.Should().Be("i3");

                    await dbContextExtensions.WithinTransaction(dbContext4, async () =>
                    {
                        var instance4 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext4, lockTimeout);
                        instance4.Should().NotBeNull();
                        instance4.InstanceId.Should().Be("i4");

                        await dbContextExtensions.WithinTransaction(dbContext5, async () =>
                        {
                            var instanceNull = await dbContextExtensions.TryLockNextInstanceAsync(dbContext5, lockTimeout);
                            instanceNull.Should().BeNull();
                        });
                    });
                });
            });
        });

        await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
            DateTime.UtcNow,
            OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);
    }

    [SkippableFact]
    public async Task TryLockNextInstanceAsync_AllQueues()
    {
        var taskHubClient = _host.Services.GetService<TaskHubClient>();

        await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
            DateTime.UtcNow,
            OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);

        var queues = new List<string>();

        for (var i = 1; i <= 4; i++)
        {
            await taskHubClient.CreateOrchestrationInstanceAsync($"o{i}", "", $"i{i}", null);
            queues.Add($"o{i}");
        }

        var dbContextFactory = _host.Services.GetService<IDbContextFactory<OrchestrationDbContext>>();
        var dbContextExtensions = _host.Services.GetService<OrchestrationDbContextExtensions>();

        using var dbContext1 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext2 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext3 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext4 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext5 = await dbContextFactory.CreateDbContextAsync();

        var lockTimeout = TimeSpan.FromMinutes(1);

        var queuesArray = queues.ToArray();

        await dbContextExtensions.WithinTransaction(dbContext1, async () =>
        {
            var instance1 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext1, queuesArray, lockTimeout);
            instance1.Should().NotBeNull();
            instance1.InstanceId.Should().Be("i1");

            await dbContextExtensions.WithinTransaction(dbContext2, async () =>
            {
                var instance2 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext2, queuesArray, lockTimeout);
                instance2.Should().NotBeNull();
                instance2.InstanceId.Should().Be("i2");

                await dbContextExtensions.WithinTransaction(dbContext3, async () =>
                {
                    var instance3 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext3, queuesArray, lockTimeout);
                    instance3.Should().NotBeNull();
                    instance3.InstanceId.Should().Be("i3");

                    await dbContextExtensions.WithinTransaction(dbContext4, async () =>
                    {
                        var instance4 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext4, queuesArray, lockTimeout);
                        instance4.Should().NotBeNull();
                        instance4.InstanceId.Should().Be("i4");

                        await dbContextExtensions.WithinTransaction(dbContext5, async () =>
                        {
                            var instanceNull = await dbContextExtensions.TryLockNextInstanceAsync(dbContext5, queuesArray, lockTimeout);
                            instanceNull.Should().BeNull();
                        });
                    });
                });
            });
        });

        await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
            DateTime.UtcNow,
            OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);
    }

    [SkippableFact]
    public async Task TryLockNextInstanceAsync_SpecificQueues()
    {
        var taskHubClient = _host.Services.GetService<TaskHubClient>();

        await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
            DateTime.UtcNow,
            OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);

        await taskHubClient.CreateOrchestrationInstanceAsync($"o1", "", $"o1-1", null);
        await taskHubClient.CreateOrchestrationInstanceAsync($"o2", "", $"o2-1", null);
        await taskHubClient.CreateOrchestrationInstanceAsync($"o3", "", $"o3-1", null);
        await taskHubClient.CreateOrchestrationInstanceAsync($"o1", "", $"o1-2", null);
        await taskHubClient.CreateOrchestrationInstanceAsync($"o2", "", $"o2-2", null);
        await taskHubClient.CreateOrchestrationInstanceAsync($"o3", "", $"o3-2", null);
        await taskHubClient.CreateOrchestrationInstanceAsync($"o1", "", $"o1-3", null);
        await taskHubClient.CreateOrchestrationInstanceAsync($"o2", "", $"o2-3", null);
        await taskHubClient.CreateOrchestrationInstanceAsync($"o3", "", $"o3-3", null);

        var dbContextFactory = _host.Services.GetService<IDbContextFactory<OrchestrationDbContext>>();
        var dbContextExtensions = _host.Services.GetService<OrchestrationDbContextExtensions>();

        using var dbContext1 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext2 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext3 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext4 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext5 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext6 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext7 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext8 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext9 = await dbContextFactory.CreateDbContextAsync();
        using var dbContext10 = await dbContextFactory.CreateDbContextAsync();

        var lockTimeout = TimeSpan.FromMinutes(1);

        await dbContextExtensions.WithinTransaction(dbContext1, async () =>
        {
            var instance3 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext1, new[] { "o3" }, lockTimeout);
            instance3.Should().NotBeNull();
            instance3.InstanceId.Should().Be("o3-1");

            await dbContextExtensions.WithinTransaction(dbContext2, async () =>
            {
                var instance2 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext2, new[] { "o2" }, lockTimeout);
                instance2.Should().NotBeNull();
                instance2.InstanceId.Should().Be("o2-1");

                await dbContextExtensions.WithinTransaction(dbContext3, async () =>
                {
                    var instance1 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext3, new[] { "o1" }, lockTimeout);
                    instance1.Should().NotBeNull();
                    instance1.InstanceId.Should().Be("o1-1");

                    await dbContextExtensions.WithinTransaction(dbContext4, async () =>
                    {
                        var instance4 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext4, new[] { "o1" }, lockTimeout);
                        instance4.Should().NotBeNull();
                        instance4.InstanceId.Should().Be("o1-2");

                        await dbContextExtensions.WithinTransaction(dbContext5, async () =>
                        {
                            var instance5 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext5, new[] { "o2" }, lockTimeout);
                            instance5.Should().NotBeNull();
                            instance5.InstanceId.Should().Be("o2-2");

                            await dbContextExtensions.WithinTransaction(dbContext6, async () =>
                            {
                                var instance6 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext6, new[] { "o3" }, lockTimeout);
                                instance6.Should().NotBeNull();
                                instance6.InstanceId.Should().Be("o3-2");

                                await dbContextExtensions.WithinTransaction(dbContext7, async () =>
                                {
                                    var instance7 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext7, new[] { "o1", "o2", "o3" }, lockTimeout);
                                    instance7.Should().NotBeNull();
                                    instance7.InstanceId.Should().Be("o1-3");

                                    await dbContextExtensions.WithinTransaction(dbContext8, async () =>
                                    {
                                        var instance8 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext8, new[] { "o1", "o2", "o3" }, lockTimeout);
                                        instance8.Should().NotBeNull();
                                        instance8.InstanceId.Should().Be("o2-3");

                                        await dbContextExtensions.WithinTransaction(dbContext9, async () =>
                                        {
                                            var instance9 = await dbContextExtensions.TryLockNextInstanceAsync(dbContext9, new[] { "o1", "o2", "o3" }, lockTimeout);
                                            instance9.Should().NotBeNull();
                                            instance9.InstanceId.Should().Be("o3-3");

                                            await dbContextExtensions.WithinTransaction(dbContext10, async () =>
                                            {
                                                var instanceNull = await dbContextExtensions.TryLockNextInstanceAsync(dbContext10, new[] { "o1", "o2", "o3" }, lockTimeout);
                                                instanceNull.Should().BeNull();
                                            });
                                        });
                                    });
                                });
                            });
                        });
                    });
                });
            });
        });

        await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
            DateTime.UtcNow,
            OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);
    }

    [Trait("Category", "Integration")]
    [SkippableFact]
    public async Task EventBetweenWorkers_ShouldBeReceived()
    {
        using var secondHost = await Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddFilter(l => l >= LogLevel.Warning).AddXUnit(_output);
            })
            .ConfigureServices(services =>
            {
                ConfigureStorage(services);

                services.AddDurableTaskClient();

                services.AddDurableTaskWorker(builder =>
                {
                    builder.AddOrchestration<SendEventOrchestration>(SendEventOrchestration.Name, SendEventOrchestration.Version);
                });
            }).StartAsync();

        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        var eventData = Guid.NewGuid();

        var instance = await taskHubClient.CreateOrchestrationInstanceAsync(WaitForEventOrchestration.Name, WaitForEventOrchestration.Version, null);

        await taskHubClient.CreateOrchestrationInstanceAsync(SendEventOrchestration.Name, SendEventOrchestration.Version, new SendEventOrchestration.Input
        {
            TargetInstanceId = instance.InstanceId,
            EventName = "SetResult",
            EventInput = eventData
        });

        var state = await taskHubClient.WaitForOrchestrationAsync(instance, FastWaitTimeout);

        state.Should().NotBeNull();
        state.Output.Should().Be($"\"{eventData}\"");
        state.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);

        await secondHost.StopAsync();
        await secondHost.WaitForShutdownAsync();
    }
}
