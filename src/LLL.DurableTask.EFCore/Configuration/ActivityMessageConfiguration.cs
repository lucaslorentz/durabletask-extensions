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

            builder.Property(x => x.InstanceId).HasMaxLength(250).IsRequired();
            builder.HasOne(x => x.Instance)
                .WithMany()
                .HasForeignKey(x => x.InstanceId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Queue).HasMaxLength(250).IsRequired();
            builder.Property(x => x.ReplyQueue).HasMaxLength(250).IsRequired();

            builder.Property(x => x.Message).HasMaxLength(int.MaxValue);

            builder.Property(x => x.CreatedAt).IsRequired().HasConversion(new UtcDateTimeConverter());
            builder.Property(x => x.LockedUntil).HasConversion(new UtcDateTimeConverter());
            builder.Property(x => x.LockId).HasMaxLength(100).IsConcurrencyToken();

            builder.HasIndex(x => new { x.LockedUntil, x.Queue });
            builder.HasIndex(x => new { x.LockedUntil });
        }
    }
}
