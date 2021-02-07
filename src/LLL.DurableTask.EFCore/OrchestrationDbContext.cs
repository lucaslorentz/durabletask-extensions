using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore
{
    public class OrchestrationDbContext : DbContext
    {
        public OrchestrationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Instance> Instances { get; set; }
        public DbSet<DurableTask.EFCore.Entities.Execution> Executions { get; set; }
        public DbSet<Entities.Event> Events { get; set; }
        public DbSet<OrchestrationBatch> OrchestrationBatches { get; set; }
        public DbSet<Entities.OrchestrationMessage> OrchestrationMessages { get; set; }
        public DbSet<Entities.ActivityMessage> ActivityMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrchestrationDbContext).Assembly);
        }
    }
}
