using LLL.DurableTask.EFCore.Configuration.Converters;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.DurableTask.EFCore.Configuration
{
    public class InstanceConfiguration : IEntityTypeConfiguration<Instance>
    {
        public void Configure(EntityTypeBuilder<Instance> builder)
        {
            builder.HasKey(x => x.InstanceId);
            builder.Property(x => x.InstanceId).HasMaxLength(500).IsRequired();

            builder.Property(x => x.LastExecutionId).HasMaxLength(100).IsRequired();
            builder.HasOne(x => x.LastExecution)
                .WithMany()
                .HasForeignKey(x => x.LastExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.LastQueueName).HasMaxLength(500).IsRequired();
            builder.Property(x => x.AvailableAt).IsRequired().HasConversion(new UtcDateTimeConverter());
            builder.Property(x => x.LockId).HasMaxLength(100).IsConcurrencyToken();

            builder.HasIndex(x => new { x.AvailableAt });
        }
    }
}
