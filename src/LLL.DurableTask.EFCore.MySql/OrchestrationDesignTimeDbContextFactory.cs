using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LLL.DurableTask.EFCore.MySql;

[ExcludeFromCodeCoverage]
public class OrchestrationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrchestrationDbContext>
{
    public OrchestrationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<OrchestrationDbContext>();
        var connectionString = "server=localhost;database=durabletask;user=root;password=root";
        builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysqlOptions =>
        {
            var assemblyName = typeof(OrchestrationDesignTimeDbContextFactory).Assembly.GetName().Name;
            mysqlOptions.MigrationsAssembly(assemblyName);
        });
        return new OrchestrationDbContext(builder.Options);
    }
}
