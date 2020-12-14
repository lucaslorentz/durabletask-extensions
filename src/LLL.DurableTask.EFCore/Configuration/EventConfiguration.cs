using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.DurableTask.EFCore.Configuration
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).IsRequired();

            builder.Property(x => x.InstanceId).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ExecutionId).HasMaxLength(100).IsRequired();

            builder.Property(x => x.SequenceNumber).IsRequired();

            builder.Property(x => x.Content).HasMaxLength(int.MaxValue).IsRequired();

            builder.HasIndex(x => new { x.InstanceId, x.ExecutionId, x.SequenceNumber }).IsUnique();
        }
    }
}
