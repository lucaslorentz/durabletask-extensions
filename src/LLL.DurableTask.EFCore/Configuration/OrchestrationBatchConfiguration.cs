using LLL.DurableTask.EFCore.Configuration.Converters;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.DurableTask.EFCore.Configuration
{
    public class OrchestrationBatchConfiguration : IEntityTypeConfiguration<OrchestrationBatch>
    {
        public void Configure(EntityTypeBuilder<OrchestrationBatch> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasMaxLength(36).IsRequired();

            builder.Property(x => x.InstanceId).HasMaxLength(250).IsRequired();
            builder.HasOne(x => x.Instance)
                .WithMany()
                .HasForeignKey(x => x.InstanceId)
                .IsRequired();

            builder.Property(x => x.Queue).HasMaxLength(250).IsRequired();

            builder.Property(x => x.AvailableAt).IsRequired().HasConversion(new UtcDateTimeConverter());

            builder.Property(x => x.LockedUntil).IsRequired().HasConversion(new UtcDateTimeConverter());
            builder.Property(x => x.LockId).HasMaxLength(100).IsConcurrencyToken();

            builder.HasIndex(x => new { x.AvailableAt, x.LockedUntil, x.Queue });

            builder.HasIndex(x => new { x.InstanceId, x.Queue }).IsUnique();
        }
    }
}
