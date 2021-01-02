using System;
using System.Threading.Tasks;
using DurableTask.Core;
using FluentAssertions;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LLL.DurableTask.Tests.Storages
{
    public abstract class EFCoreTestBase : StorageTestBase
    {
        protected EFCoreTestBase(ITestOutputHelper output) : base(output)
        {
        }

        [SkippableFact]
        public async Task LockNextInstanceForUpdate_AnyQueue()
        {
            var taskHubClient = _host.Services.GetService<TaskHubClient>();

            await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
                DateTime.UtcNow,
                OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);

            for (var i = 1; i <= 4; i++)
                await taskHubClient.CreateOrchestrationInstanceAsync($"o{i}", "", $"i{i}", null);

            var dbContextFactory = _host.Services.GetService<Func<OrchestrationDbContext>>();
            var dbContextExtensions = _host.Services.GetService<OrchestrationDbContextExtensions>();

            using var dbContextDispenser = new DbContextDispenser(dbContextFactory, dbContextExtensions);

            var instance1 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get());
            var instance2 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get());
            var instance3 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get());
            var instance4 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get());
            var instanceNull = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get());

            instance1.Should().NotBeNull();
            instance1.InstanceId.Should().Be("i1");
            instance2.Should().NotBeNull();
            instance2.InstanceId.Should().Be("i2");
            instance3.Should().NotBeNull();
            instance3.InstanceId.Should().Be("i3");
            instance4.Should().NotBeNull();
            instance4.InstanceId.Should().Be("i4");
            instanceNull.Should().BeNull();
        }

        [SkippableFact]
        public async Task LockNextInstanceForUpdate_SpecificQueues()
        {
            var taskHubClient = _host.Services.GetService<TaskHubClient>();

            await taskHubClient.PurgeOrchestrationInstanceHistoryAsync(
                DateTime.UtcNow,
                OrchestrationStateTimeRangeFilterType.OrchestrationCreatedTimeFilter);

            await taskHubClient.CreateOrchestrationInstanceAsync($"o1", "", $"i1", null);
            await taskHubClient.CreateOrchestrationInstanceAsync($"o2", "", $"i2", null);
            await taskHubClient.CreateOrchestrationInstanceAsync($"o3", "", $"i3", null);
            await taskHubClient.CreateOrchestrationInstanceAsync($"o1", "", $"i4", null);
            await taskHubClient.CreateOrchestrationInstanceAsync($"o2", "", $"i5", null);
            await taskHubClient.CreateOrchestrationInstanceAsync($"o3", "", $"i6", null);
            await taskHubClient.CreateOrchestrationInstanceAsync($"o1", "", $"i7", null);
            await taskHubClient.CreateOrchestrationInstanceAsync($"o2", "", $"i8", null);
            await taskHubClient.CreateOrchestrationInstanceAsync($"o3", "", $"i9", null);

            var dbContextFactory = _host.Services.GetService<Func<OrchestrationDbContext>>();
            var dbContextExtensions = _host.Services.GetService<OrchestrationDbContextExtensions>();

            using var dbContextDispenser = new DbContextDispenser(dbContextFactory, dbContextExtensions);

            var instance3 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o3" });
            var instance2 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o2" });
            var instance1 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o1" });
            var instance4 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o1" });
            var instance5 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o2" });
            var instance6 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o3" });
            var instance7 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o1", "o2", "o3" });
            var instance8 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o1", "o2", "o3" });
            var instance9 = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o1", "o2", "o3" });
            var instanceNull = await dbContextExtensions.LockNextInstanceForUpdate(await dbContextDispenser.Get(), new[] { "o1", "o2", "o3" });

            instance1.Should().NotBeNull();
            instance1.InstanceId.Should().Be("i1");
            instance2.Should().NotBeNull();
            instance2.InstanceId.Should().Be("i2");
            instance3.Should().NotBeNull();
            instance3.InstanceId.Should().Be("i3");
            instance4.Should().NotBeNull();
            instance4.InstanceId.Should().Be("i4");
            instance5.Should().NotBeNull();
            instance5.InstanceId.Should().Be("i5");
            instance6.Should().NotBeNull();
            instance6.InstanceId.Should().Be("i6");
            instance7.Should().NotBeNull();
            instance7.InstanceId.Should().Be("i7");
            instance8.Should().NotBeNull();
            instance8.InstanceId.Should().Be("i8");
            instance9.Should().NotBeNull();
            instance9.InstanceId.Should().Be("i9");
            instanceNull.Should().BeNull();
        }
    }
}
