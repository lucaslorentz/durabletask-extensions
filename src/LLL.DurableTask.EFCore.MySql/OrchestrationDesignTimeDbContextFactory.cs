using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LLL.DurableTask.EFCore.MySql
{
    public class OrchestrationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrchestrationDbContext>
    {
        public OrchestrationDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .Build();

            var builder = new DbContextOptionsBuilder<OrchestrationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            builder.UseMySql("server=localhost;database=durabletask;user=root;password=root", mysqlOptions =>
            {
                var assemblyName = typeof(OrchestrationDesignTimeDbContextFactory).Assembly.GetName().Name;
                mysqlOptions.MigrationsAssembly(assemblyName);
            });
            return new OrchestrationDbContext(builder.Options);
        }
    }
}
