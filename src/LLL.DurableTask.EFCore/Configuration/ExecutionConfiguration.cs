using DurableTask.Core;
using LLL.DurableTask.EFCore.Configuration.Converters;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LLL.DurableTask.EFCore.Configuration
{
    public class ExecutionConfiguration : IEntityTypeConfiguration<Execution>
    {
        public void Configure(EntityTypeBuilder<Execution> builder)
        {
            builder.HasKey(x => x.ExecutionId);
            builder.Property(x => x.ExecutionId).HasMaxLength(100).IsRequired();

            builder.Property(x => x.InstanceId).HasMaxLength(250).IsRequired();

            builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
            builder.Property(x => x.Version).HasMaxLength(100).IsRequired();

            builder.Property(x => x.CreatedTime).IsRequired().HasConversion(new UtcDateTimeConverter());
            builder.Property(x => x.CompletedTime).IsRequired().HasConversion(new UtcDateTimeConverter());
            builder.Property(x => x.LastUpdatedTime).IsRequired().HasConversion(new UtcDateTimeConverter());

            builder.Property(x => x.CompressedSize).IsRequired();
            builder.Property(x => x.Size).IsRequired();

            builder.Property(x => x.Status).IsRequired().HasConversion(new EnumToStringConverter<OrchestrationStatus>());
            builder.Property(x => x.FailureDetails).HasMaxLength(int.MaxValue);
            builder.Property(x => x.CustomStatus);

            builder.Property(x => x.ParentInstance).HasMaxLength(2000);
            builder.OwnsMany(x => x.Tags, o =>
            {
                o.ToTable("ExecutionTags");
                o.WithOwner().HasForeignKey("ExecutionId");
                o.Property(x => x.Name).IsRequired().HasMaxLength(100);
                o.Property(x => x.Value).IsRequired().HasMaxLength(500);
                o.HasIndex("ExecutionId", "Name").IsUnique();
                o.HasIndex(x => new { x.Name, x.Value });
            });

            builder.Property(x => x.Input);
            builder.Property(x => x.Output);

            builder.HasIndex(x => new { x.CreatedTime, x.InstanceId })
                .IsDescending(true, true);

            builder.HasIndex(x => new { x.CreatedTime });

            builder.HasIndex(x => new { x.InstanceId });

            builder.HasIndex(x => new { x.Name });

            builder.HasIndex(x => new { x.Status });
        }
    }
}
