using LLL.DurableTask.EFCore.Configuration.Converters;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.DurableTask.EFCore.Configuration
{
    public class ActivityMessageConfiguration : IEntityTypeConfiguration<ActivityMessage>
    {
        public void Configure(EntityTypeBuilder<ActivityMessage> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).IsRequired();

            builder.Property(x => x.InstanceId).HasMaxLength(100).IsRequired();
            builder.HasOne(x => x.Instance).WithMany().HasForeignKey(x => new { x.InstanceId }).IsRequired();

            builder.Property(x => x.ExecutionId).HasMaxLength(100).IsRequired();

            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Version).HasMaxLength(100).IsRequired();

            builder.Property(x => x.CreatedAt).IsRequired().HasConversion(new UtcDateTimeConverter());

            builder.Property(x => x.Message).HasMaxLength(int.MaxValue);

            builder.Property(x => x.Queue).HasMaxLength(100).IsRequired();
            builder.Property(x => x.AvailableAt).HasConversion(new UtcDateTimeConverter());
            builder.Property(x => x.LockId).HasMaxLength(100).IsConcurrencyToken();

            builder.HasIndex(x => new { x.Queue, x.AvailableAt });
            builder.HasIndex(x => new { x.AvailableAt });
        }
    }
}
