using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using DurableTask.Core;
using DurableTask.Core.Exceptions;
using DurableTask.Core.History;
using LLL.DurableTask.Core;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.EFCore.Mappers;
using LLL.DurableTask.Tests.Storage.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    protected override async Task AssertNoPendingMessagesAsync(string instanceId)
    {
        var dbContextFactory = _host.Services.GetRequiredService<IDbContextFactory<OrchestrationDbContext>>();

        var deadline = DateTime.UtcNow + FastWaitTimeout;
        while (true)
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var pending = await dbContext.OrchestrationMessages
                .CountAsync(m => m.InstanceId == instanceId);

            if (pending == 0 || DateTime.UtcNow >= deadline)
            {
                pending.Should().Be(0, "a terminated orchestration should leave no pending messages");
                return;
            }

            await Task.Delay(50);
        }
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

    private record ActivityNameVersion(string Name, string Version) : INameVersionInfo;

    // Locks an activity message that no worker handles, then deletes it, the way a purge would
    private async Task<(TaskActivityWorkItem WorkItem, OrchestrationInstance Instance)> LockAndDeleteActivityMessageAsync()
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();
        var dbContextFactory = _host.Services.GetRequiredService<IDbContextFactory<OrchestrationDbContext>>();
        var mapper = _host.Services.GetRequiredService<ActivityMessageMapper>();
        var service = _host.Services.GetRequiredService<IDistributedOrchestrationService>();

        // Names not registered in the worker, so only this test consumes the activity
        var instance = await taskHubClient.CreateOrchestrationInstanceAsync("lost-lock-orchestration", "", null);

        var activityMessage = mapper.CreateActivityMessage(new TaskMessage
        {
            OrchestrationInstance = instance,
            Event = new TaskScheduledEvent(0) { Name = "lost-lock-activity", Version = "" }
        }, "lost-lock-orchestration");

        using (var dbContext = await dbContextFactory.CreateDbContextAsync())
        {
            dbContext.ActivityMessages.Add(activityMessage);
            await dbContext.SaveChangesAsync();
        }

        var workItem = await service.LockNextTaskActivityWorkItem(
            TimeSpan.FromSeconds(5),
            new INameVersionInfo[] { new ActivityNameVersion("lost-lock-activity", "") },
            CancellationToken.None);
        workItem.Should().NotBeNull();

        using (var dbContext = await dbContextFactory.CreateDbContextAsync())
        {
            dbContext.ActivityMessages.Remove(await dbContext.ActivityMessages.FindAsync(activityMessage.Id));
            await dbContext.SaveChangesAsync();
        }

        return (workItem, instance);
    }

    [SkippableFact]
    public async Task AbandonTaskActivityWorkItem_WhenMessageWasDeleted_ShouldNotThrow()
    {
        var (workItem, _) = await LockAndDeleteActivityMessageAsync();
        var service = _host.Services.GetRequiredService<IOrchestrationService>();

        await service.Invoking(s => s.AbandonTaskActivityWorkItemAsync(workItem))
            .Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task CompleteTaskActivityWorkItem_WhenMessageWasDeleted_ShouldNotThrow()
    {
        var (workItem, instance) = await LockAndDeleteActivityMessageAsync();
        var service = _host.Services.GetRequiredService<IOrchestrationService>();

        await service.Invoking(s => s.CompleteTaskActivityWorkItemAsync(workItem, new TaskMessage
        {
            OrchestrationInstance = instance,
            Event = new TaskCompletedEvent(-1, 0, "null")
        })).Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task RenewTaskActivityWorkItemLock_WhenMessageWasDeleted_ShouldReportLostLock()
    {
        var (workItem, _) = await LockAndDeleteActivityMessageAsync();
        var service = _host.Services.GetRequiredService<IOrchestrationService>();

        var exception = await Record.ExceptionAsync(() => service.RenewTaskActivityWorkItemLockAsync(workItem));

        exception.Should().NotBeNull();
        exception.Should().NotBeOfType<NullReferenceException>();
        exception.Message.Should().Contain("Lost task activity lock");
    }

    [Trait("Category", "Integration")]
    [SkippableFact]
    public async Task SendEventToMissingInstance_ShouldNotBlockOrchestrationCompletion()
    {
        // The main host does not register SendEventOrchestration, so a second one runs it
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

        var instance = await taskHubClient.CreateOrchestrationInstanceAsync(SendEventOrchestration.Name, SendEventOrchestration.Version, new SendEventOrchestration.Input
        {
            TargetInstanceId = Guid.NewGuid().ToString(),
            EventName = "SetResult",
            EventInput = "ignored"
        });

        var state = await taskHubClient.WaitForOrchestrationAsync(instance, FastWaitTimeout);

        state.Should().NotBeNull();
        state.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);

        await secondHost.StopAsync();
        await secondHost.WaitForShutdownAsync();
    }

    [Trait("Category", "Integration")]
    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PurgeInstanceStateWithFilter_ShouldIncludeBoundsAndAcceptEmptyStatuses(bool nullStatuses)
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();
        var purgeClient = _host.Services.GetRequiredService<IOrchestrationServicePurgeClient>();

        var instance = await taskHubClient.CreateOrchestrationInstanceAsync(
            EmptyOrchestration.Name,
            EmptyOrchestration.Version,
            string.Empty);

        // Wait for completion so the worker is not holding row locks that purge would skip
        await taskHubClient.WaitForOrchestrationAsync(instance, FastWaitTimeout);

        var state = await taskHubClient.GetOrchestrationStateAsync(instance.InstanceId);
        state.Should().NotBeNull();

        // Both bounds equal to the stored creation time: only matches if they are inclusive
        var filter = new PurgeInstanceFilterExtended(
            state.CreatedTime,
            state.CreatedTime,
            nullStatuses ? null : Array.Empty<OrchestrationStatus>());

        var purgeResult = await purgeClient.PurgeInstanceStateAsync(filter);
        purgeResult.DeletedInstanceCount.Should().BeGreaterThan(0);

        var stateAfterPurge = await taskHubClient.GetOrchestrationStateAsync(instance.InstanceId);
        stateAfterPurge.Should().BeNull();
    }

    [SkippableFact]
    public async Task CompleteTaskOrchestrationWorkItem_ShouldExtendInstanceLock()
    {
        using var _ = UseOrchestrationLockTimeout(TimeSpan.FromSeconds(5));
        var (workItem, instance) = await LockOrchestrationAsync("extend-lock-orchestration");
        var lockedUntilBefore = workItem.LockedUntilUtc;

        // An extended session keeps processing the instance well past the original lock
        await Task.Delay(TimeSpan.FromSeconds(2));

        await CompleteOrchestrationAsync(workItem, instance, "extend-lock-orchestration");

        var (_, lockedUntilAfter) = await GetInstanceLockAsync(instance.InstanceId);
        lockedUntilAfter.Should().BeAfter(lockedUntilBefore.AddSeconds(1));
        workItem.LockedUntilUtc.Should().BeCloseTo(lockedUntilAfter, TimeSpan.FromSeconds(1));

        await ReleaseAndPurgeAsync(workItem);
    }

    [SkippableFact]
    public async Task FetchNewOrchestrationMessages_ShouldExtendInstanceLock()
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();

        using var _ = UseOrchestrationLockTimeout(TimeSpan.FromSeconds(5));
        var (workItem, instance) = await LockOrchestrationAsync("fetch-extend-lock-orchestration");
        var lockedUntilBefore = workItem.LockedUntilUtc;

        // The lock is only extended once less than half of it is left
        await Task.Delay(TimeSpan.FromSeconds(3));

        await taskHubClient.RaiseEventAsync(
            new OrchestrationInstance { InstanceId = instance.InstanceId },
            "event",
            "data");

        var messages = await workItem.Session.FetchNewOrchestrationMessagesAsync(workItem);
        messages.Should().ContainSingle();

        var (_, lockedUntilAfter) = await GetInstanceLockAsync(instance.InstanceId);
        lockedUntilAfter.Should().BeAfter(lockedUntilBefore.AddSeconds(1));
        workItem.LockedUntilUtc.Should().BeCloseTo(lockedUntilAfter, TimeSpan.FromSeconds(1));

        await ReleaseAndPurgeAsync(workItem);
    }

    [SkippableFact]
    public async Task CompleteTaskOrchestrationWorkItem_WhenLockWasLost_ShouldAbortSessionAndKeepOtherLock()
    {
        var orchestrationService = _host.Services.GetRequiredService<IOrchestrationService>();

        var (workItem, instance) = await LockOrchestrationAsync("lost-lock-orchestration");
        var (thiefLockId, thiefLockedUntil) = await StealInstanceLockAsync(instance.InstanceId);

        await orchestrationService
            .Invoking(s => CompleteOrchestrationAsync(workItem, instance, "lost-lock-orchestration"))
            .Should().ThrowAsync<SessionAbortedException>();

        // DTFx abandons the aborted session, which must not touch the lock of its successor
        await orchestrationService.AbandonTaskOrchestrationWorkItemAsync(workItem);

        var (lockId, lockedUntil) = await GetInstanceLockAsync(instance.InstanceId);
        lockId.Should().Be(thiefLockId);
        lockedUntil.Should().BeCloseTo(thiefLockedUntil, TimeSpan.FromSeconds(1));

        await PurgeAsync();
    }

    [SkippableFact]
    public async Task ReleaseTaskOrchestrationWorkItem_WhenLockWasLost_ShouldKeepOtherLock()
    {
        var orchestrationService = _host.Services.GetRequiredService<IOrchestrationService>();

        var (workItem, instance) = await LockOrchestrationAsync("lost-lock-release-orchestration");
        var (thiefLockId, thiefLockedUntil) = await StealInstanceLockAsync(instance.InstanceId);

        await orchestrationService.ReleaseTaskOrchestrationWorkItemAsync(workItem);

        var (lockId, lockedUntil) = await GetInstanceLockAsync(instance.InstanceId);
        lockId.Should().Be(thiefLockId);
        lockedUntil.Should().BeCloseTo(thiefLockedUntil, TimeSpan.FromSeconds(1));

        await PurgeAsync();
    }

    [SkippableFact]
    public async Task RenewTaskOrchestrationWorkItemLock_WhenLockWasLost_ShouldAbortSession()
    {
        var orchestrationService = _host.Services.GetRequiredService<IOrchestrationService>();

        var (workItem, instance) = await LockOrchestrationAsync("lost-lock-renew-orchestration");
        var (thiefLockId, thiefLockedUntil) = await StealInstanceLockAsync(instance.InstanceId);

        await orchestrationService
            .Invoking(s => s.RenewTaskOrchestrationWorkItemLockAsync(workItem))
            .Should().ThrowAsync<SessionAbortedException>();

        var (lockId, lockedUntil) = await GetInstanceLockAsync(instance.InstanceId);
        lockId.Should().Be(thiefLockId);
        lockedUntil.Should().BeCloseTo(thiefLockedUntil, TimeSpan.FromSeconds(1));

        await PurgeAsync();
    }

    // Orchestration names used by these tests are not registered in the worker, so only the test consumes them
    private async Task<(TaskOrchestrationWorkItem WorkItem, OrchestrationInstance Instance)> LockOrchestrationAsync(string name)
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();
        var service = _host.Services.GetRequiredService<IDistributedOrchestrationService>();

        var instance = await taskHubClient.CreateOrchestrationInstanceAsync(name, "", null);

        var workItem = await service.LockNextTaskOrchestrationWorkItemAsync(
            TimeSpan.FromSeconds(5),
            new INameVersionInfo[] { new OrchestrationNameVersion(name, "") },
            CancellationToken.None);
        workItem.Should().NotBeNull();

        // The dispatcher applies the new messages to the runtime state before completing
        foreach (var message in workItem.NewMessages)
            workItem.OrchestrationRuntimeState.AddEvent(message.Event);

        return (workItem, instance);
    }

    private Task CompleteOrchestrationAsync(TaskOrchestrationWorkItem workItem, OrchestrationInstance instance, string name)
    {
        var orchestrationService = _host.Services.GetRequiredService<IOrchestrationService>();

        return orchestrationService.CompleteTaskOrchestrationWorkItemAsync(
            workItem,
            workItem.OrchestrationRuntimeState,
            new List<TaskMessage>(),
            new List<TaskMessage>(),
            new List<TaskMessage>(),
            null,
            new OrchestrationState
            {
                OrchestrationInstance = instance,
                Name = name,
                Version = "",
                OrchestrationStatus = OrchestrationStatus.Running
            });
    }

    private async Task<(string LockId, DateTime LockedUntil)> GetInstanceLockAsync(string instanceId)
    {
        var dbContextFactory = _host.Services.GetRequiredService<IDbContextFactory<OrchestrationDbContext>>();
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var instance = await dbContext.Instances.AsNoTracking().FirstAsync(i => i.InstanceId == instanceId);
        return (instance.LockId, instance.LockedUntil);
    }

    // Simulates another worker taking over the instance after this session's lock expired
    private async Task<(string LockId, DateTime LockedUntil)> StealInstanceLockAsync(string instanceId)
    {
        var dbContextFactory = _host.Services.GetRequiredService<IDbContextFactory<OrchestrationDbContext>>();
        using (var dbContext = await dbContextFactory.CreateDbContextAsync())
        {
            var instance = await dbContext.Instances.FirstAsync(i => i.InstanceId == instanceId);
            instance.LockId = Guid.NewGuid().ToString();
            instance.LockedUntil = DateTime.UtcNow.AddMinutes(5);
            await dbContext.SaveChangesAsync();
        }

        return await GetInstanceLockAsync(instanceId);
    }

    private async Task ReleaseAndPurgeAsync(TaskOrchestrationWorkItem workItem)
    {
        var orchestrationService = _host.Services.GetRequiredService<IOrchestrationService>();
        await orchestrationService.ReleaseTaskOrchestrationWorkItemAsync(workItem);
        await PurgeAsync();
    }

    private async Task PurgeAsync()
    {
        var taskHubClient = _host.Services.GetRequiredService<TaskHubClient>();
        await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
            DateTime.UtcNow,
            OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);
    }

    private IDisposable UseOrchestrationLockTimeout(TimeSpan timeout)
    {
        var options = _host.Services.GetRequiredService<IOptions<EFCoreOrchestrationOptions>>().Value;
        var original = options.OrchestrationLockTimeout;
        options.OrchestrationLockTimeout = timeout;
        return new RestoreAction(() => options.OrchestrationLockTimeout = original);
    }

    private sealed class RestoreAction : IDisposable
    {
        private readonly Action _action;
        public RestoreAction(Action action) => _action = action;
        public void Dispose() => _action();
    }

    private record OrchestrationNameVersion(string Name, string Version) : INameVersionInfo;

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
