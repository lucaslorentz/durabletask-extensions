using System;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.EFCore.DependencyInjection;
using LLL.DurableTask.EFCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PostgreSqlEFCoreOrchestrationBuilderExtensions
    {
        public static IEFCoreOrchestrationBuilder UseNpgsql(
            this IEFCoreOrchestrationBuilder builder,
            string connectionString,
            Action<NpgsqlDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            builder.Services.AddSingleton<OrchestrationDbContextExtensions, PostgreOrchestrationDbContextExtensions>();

            return builder.ConfigureDbContext(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    var assemblyName = typeof(PostgreSqlEFCoreOrchestrationBuilderExtensions).Assembly.GetName().Name;
                    npgsqlOptions.MigrationsAssembly(assemblyName);
                    mysqlOptionsAction?.Invoke(npgsqlOptions);
                });
            });
        }
    }
}
