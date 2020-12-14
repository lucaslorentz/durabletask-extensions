using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LLL.DurableTask.EFCore.SqlServer
{
    public class OrchestrationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrchestrationDbContext>
    {
        public OrchestrationDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .Build();

            var builder = new DbContextOptionsBuilder<OrchestrationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            builder.UseSqlServer("server=localhost;database=durabletask;user=sa;password=P1ssw0rd", sqlServerOptions =>
            {
                var assemblyName = typeof(OrchestrationDesignTimeDbContextFactory).Assembly.GetName().Name;
                sqlServerOptions.MigrationsAssembly(assemblyName);
            });
            return new OrchestrationDbContext(builder.Options);
        }
    }
}
