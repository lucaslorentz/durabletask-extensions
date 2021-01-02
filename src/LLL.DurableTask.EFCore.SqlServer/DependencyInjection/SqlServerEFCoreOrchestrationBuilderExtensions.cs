using System;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.EFCore.DependencyInjection;
using LLL.DurableTask.EFCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqlServerEFCoreOrchestrationBuilderExtensions
    {
        public static IEFCoreOrchestrationBuilder UseSqlServer(
            this IEFCoreOrchestrationBuilder builder,
            string connectionString,
            Action<SqlServerDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            builder.Services.AddSingleton<OrchestrationDbContextExtensions, SqlServerOrchestrationDbContextExtensions>();

            return builder.ConfigureDbContext(options =>
            {
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    var assemblyName = typeof(SqlServerEFCoreOrchestrationBuilderExtensions).Assembly.GetName().Name;
                    sqlServerOptions.MigrationsAssembly(assemblyName);
                    mysqlOptionsAction?.Invoke(sqlServerOptions);
                });
            });
        }
    }
}
