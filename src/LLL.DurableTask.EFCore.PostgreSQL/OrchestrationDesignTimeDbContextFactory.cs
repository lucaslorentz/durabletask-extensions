using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LLL.DurableTask.EFCore.PostgreSQL;

[ExcludeFromCodeCoverage]
public class OrchestrationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrchestrationDbContext>
{
    public OrchestrationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<OrchestrationDbContext>();
        builder.UseNpgsql("Server=localhost;Port=5432;Database=durabletask;User Id=postgres;Password=root", mysqlOptions =>
        {
            var assemblyName = typeof(OrchestrationDesignTimeDbContextFactory).Assembly.GetName().Name;
            mysqlOptions.MigrationsAssembly(assemblyName);
        });
        return new OrchestrationDbContext(builder.Options);
    }
}
