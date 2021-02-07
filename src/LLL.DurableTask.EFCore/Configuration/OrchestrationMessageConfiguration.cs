﻿using LLL.DurableTask.EFCore.Configuration.Converters;
using LLL.DurableTask.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LLL.DurableTask.EFCore.Configuration
{
    public class OrchestrationMessageConfiguration : IEntityTypeConfiguration<OrchestrationMessage>
    {
        public void Configure(EntityTypeBuilder<OrchestrationMessage> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasMaxLength(36).IsRequired();

            builder.Property(x => x.BatchId).HasMaxLength(36).IsRequired();
            builder.HasOne(x => x.Batch)
                .WithMany()
                .HasForeignKey(x => x.BatchId)
                .IsRequired();

            builder.Property(x => x.ExecutionId).HasMaxLength(100);

            builder.Property(x => x.AvailableAt).IsRequired().HasConversion(new UtcDateTimeConverter());

            builder.Property(x => x.SequenceNumber).IsRequired();

            builder.Property(x => x.Message).HasMaxLength(int.MaxValue);
        }
    }
}
