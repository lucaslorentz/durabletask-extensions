using LLL.DurableTask.EFCore.Configuration.Converters;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.DurableTask.EFCore.Configuration;

public class OrchestrationMessageConfiguration : IEntityTypeConfiguration<OrchestrationMessage>
{
    public void Configure(EntityTypeBuilder<OrchestrationMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36).IsRequired();

        builder.Property(x => x.InstanceId).HasMaxLength(250).IsRequired();
        builder.HasOne(x => x.Instance)
            .WithMany()
            .HasForeignKey(x => x.InstanceId)
            .IsRequired();

        builder.Property(x => x.ExecutionId).HasMaxLength(100);

        builder.Property(x => x.AvailableAt).IsRequired().HasConversion(new UtcDateTimeConverter());

        builder.Property(x => x.SequenceNumber).IsRequired();

        builder.Property(x => x.Message).HasMaxLength(int.MaxValue);

        // This allows locking next message instance via index scan, not locking extra rows
        builder.HasIndex(x => new { x.AvailableAt, x.Queue, x.InstanceId });
    }
}
