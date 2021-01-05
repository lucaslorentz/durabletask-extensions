using System;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.EFCore.DependencyInjection;
using LLL.DurableTask.EFCore.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InMemoryEFCoreOrchestrationBuilderExtensions
    {
        public static IEFCoreOrchestrationBuilder UseInMemoryDatabase(
            this IEFCoreOrchestrationBuilder builder,
            string databaseName,
            Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null)
        {
            builder.Services.AddSingleton<OrchestrationDbContextExtensions, InMemoryOrchestrationDbContextExtensions>();

            return builder.ConfigureDbContext(options =>
            {
                options.UseInMemoryDatabase(databaseName, inMemoryOptions =>
                {
                    inMemoryOptionsAction?.Invoke(inMemoryOptions);
                });
            });
        }
    }
}
