using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LLL.DurableTask.EFCore.SqlServer
{
    [ExcludeFromCodeCoverage]
    public class OrchestrationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrchestrationDbContext>
    {
        public OrchestrationDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<OrchestrationDbContext>();
            builder.UseSqlServer("server=localhost;database=durabletask;user=sa;password=P1ssw0rd;Encrypt=false", sqlServerOptions =>
            {
                var assemblyName = typeof(OrchestrationDesignTimeDbContextFactory).Assembly.GetName().Name;
                sqlServerOptions.MigrationsAssembly(assemblyName);
            });
            return new OrchestrationDbContext(builder.Options);
        }
    }
}
