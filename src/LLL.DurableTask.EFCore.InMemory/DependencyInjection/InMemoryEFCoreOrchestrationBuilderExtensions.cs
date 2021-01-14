using System;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.EFCore.DependencyInjection;
using LLL.DurableTask.EFCore.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InMemoryEFCoreOrchestrationBuilderExtensions
    {
        public static IEFCoreOrchestrationBuilder UseInMemoryDatabase(
            this IEFCoreOrchestrationBuilder builder,
            string databaseName,
            Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
        {
            return builder.UseInMemoryDatabase(databaseName, null, inMemoryOptionsAction);
        }

        public static IEFCoreOrchestrationBuilder UseInMemoryDatabase(
            this IEFCoreOrchestrationBuilder builder,
            string databaseName,
            InMemoryDatabaseRoot databaseRoot,
            Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
        {
            builder.Services.AddSingleton<OrchestrationDbContextExtensions, InMemoryOrchestrationDbContextExtensions>();

            return builder.ConfigureDbContext(options =>
            {
                options.UseInMemoryDatabase(databaseName, databaseRoot, inMemoryOptions =>
                {
                    inMemoryOptionsAction?.Invoke(inMemoryOptions);
                });
            });
        }
    }
}
