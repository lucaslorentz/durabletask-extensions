using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LLL.DurableTask.EFCore.PostgreSQL
{
    public class OrchestrationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrchestrationDbContext>
    {
        public OrchestrationDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .Build();

            var builder = new DbContextOptionsBuilder<OrchestrationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            builder.UseNpgsql("Server=localhost;Port=5432;Database=durabletask;User Id=postgres;Password=root", mysqlOptions =>
            {
                var assemblyName = typeof(OrchestrationDesignTimeDbContextFactory).Assembly.GetName().Name;
                mysqlOptions.MigrationsAssembly(assemblyName);
            });
            return new OrchestrationDbContext(builder.Options);
        }
    }
}
