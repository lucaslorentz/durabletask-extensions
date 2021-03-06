﻿// <auto-generated />
using System;
using LLL.DurableTask.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LLL.DurableTask.EFCore.MySql.Migrations
{
    [DbContext(typeof(OrchestrationDbContext))]
    partial class OrchestrationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.ActivityMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("InstanceId")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<string>("LockId")
                        .IsConcurrencyToken()
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<DateTime>("LockedUntil")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Message")
                        .HasColumnType("longtext CHARACTER SET utf8mb4")
                        .HasMaxLength(2147483647);

                    b.Property<string>("Queue")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<string>("ReplyQueue")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.HasKey("Id");

                    b.HasIndex("InstanceId");

                    b.HasIndex("LockedUntil");

                    b.HasIndex("LockedUntil", "Queue");

                    b.ToTable("ActivityMessages");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Event", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext CHARACTER SET utf8mb4")
                        .HasMaxLength(2147483647);

                    b.Property<string>("ExecutionId")
                        .IsRequired()
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<string>("InstanceId")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<int>("SequenceNumber")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ExecutionId");

                    b.HasIndex("InstanceId", "ExecutionId", "SequenceNumber")
                        .IsUnique();

                    b.ToTable("Events");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Execution", b =>
                {
                    b.Property<string>("ExecutionId")
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<DateTime>("CompletedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("CompressedSize")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("CustomStatus")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Input")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("InstanceId")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<DateTime>("LastUpdatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<string>("Output")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("ParentInstance")
                        .HasColumnType("varchar(2000) CHARACTER SET utf8mb4")
                        .HasMaxLength(2000);

                    b.Property<long>("Size")
                        .HasColumnType("bigint");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.HasKey("ExecutionId");

                    b.ToTable("Executions");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Instance", b =>
                {
                    b.Property<string>("InstanceId")
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<string>("LastExecutionId")
                        .IsRequired()
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<string>("LastQueueName")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<string>("LockId")
                        .IsConcurrencyToken()
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<DateTime>("LockedUntil")
                        .HasColumnType("datetime(6)");

                    b.HasKey("InstanceId");

                    b.HasIndex("LastExecutionId");

                    b.HasIndex("LockedUntil");

                    b.ToTable("Instances");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.OrchestrationMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("AvailableAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("ExecutionId")
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<string>("InstanceId")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<string>("Message")
                        .HasColumnType("longtext CHARACTER SET utf8mb4")
                        .HasMaxLength(2147483647);

                    b.Property<string>("Queue")
                        .IsRequired()
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500);

                    b.Property<int>("SequenceNumber")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AvailableAt");

                    b.HasIndex("InstanceId");

                    b.HasIndex("AvailableAt", "Queue");

                    b.ToTable("OrchestrationMessages");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.ActivityMessage", b =>
                {
                    b.HasOne("LLL.DurableTask.EFCore.Entities.Instance", "Instance")
                        .WithMany()
                        .HasForeignKey("InstanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Event", b =>
                {
                    b.HasOne("LLL.DurableTask.EFCore.Entities.Execution", "Execution")
                        .WithMany()
                        .HasForeignKey("ExecutionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Execution", b =>
                {
                    b.OwnsMany("LLL.DurableTask.EFCore.Entities.Tag", "Tags", b1 =>
                        {
                            b1.Property<string>("ExecutionId")
                                .HasColumnType("varchar(100) CHARACTER SET utf8mb4");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                                .HasMaxLength(100);

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasColumnType("varchar(2000) CHARACTER SET utf8mb4")
                                .HasMaxLength(2000);

                            b1.HasKey("ExecutionId", "Id");

                            b1.ToTable("ExecutionTags");

                            b1.WithOwner()
                                .HasForeignKey("ExecutionId");
                        });
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Instance", b =>
                {
                    b.HasOne("LLL.DurableTask.EFCore.Entities.Execution", "LastExecution")
                        .WithMany()
                        .HasForeignKey("LastExecutionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.OrchestrationMessage", b =>
                {
                    b.HasOne("LLL.DurableTask.EFCore.Entities.Instance", "Instance")
                        .WithMany()
                        .HasForeignKey("InstanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
