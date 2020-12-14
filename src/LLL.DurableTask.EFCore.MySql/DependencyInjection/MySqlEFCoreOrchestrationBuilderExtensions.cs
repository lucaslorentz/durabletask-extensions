using System;
using LLL.DurableTask.EFCore;
using LLL.DurableTask.EFCore.DependencyInjection;
using LLL.DurableTask.EFCore.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MySqlEFCoreOrchestrationBuilderExtensions
    {
        public static IEFCoreOrchestrationBuilder UseMySql(
            this IEFCoreOrchestrationBuilder builder,
            string connectionString,
            Action<MySqlDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            builder.Services.AddSingleton<EFCoreOrchestrationService, MySqlEFCoreOrchestrationService>();
            builder.Services.AddSingleton<EFCoreOrchestrationServiceClient, MySqlEFCoreOrchestrationServiceClient>();

            return builder.ConfigureDbContext(options =>
            {
                options.UseMySql(connectionString, mysqlOptions =>
                {
                    var assemblyName = typeof(MySqlEFCoreOrchestrationBuilderExtensions).Assembly.GetName().Name;
                    mysqlOptions.MigrationsAssembly(assemblyName);
                    mysqlOptionsAction?.Invoke(mysqlOptions);
                });
            });
        }
    }
}
