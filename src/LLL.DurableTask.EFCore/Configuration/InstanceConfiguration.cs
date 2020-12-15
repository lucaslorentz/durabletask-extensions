﻿using LLL.DurableTask.EFCore.Configuration.Converters;
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
            builder.Property(x => x.InstanceId).HasMaxLength(100).IsRequired();

            builder.Property(x => x.Queue).HasMaxLength(300).IsRequired();
            builder.Property(x => x.AvailableAt).IsRequired().HasConversion(new UtcDateTimeConverter());
            builder.Property(x => x.LockId).HasMaxLength(100).IsConcurrencyToken();

            builder.HasIndex(x => new { x.Queue, x.AvailableAt });
            builder.HasIndex(x => new { x.AvailableAt });
        }
    }
}