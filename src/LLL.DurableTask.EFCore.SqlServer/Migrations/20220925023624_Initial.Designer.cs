﻿// <auto-generated />
using System;
using LLL.DurableTask.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LLL.DurableTask.EFCore.SqlServer.Migrations
{
    [DbContext(typeof(OrchestrationDbContext))]
    [Migration("20220925023624_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.ActivityMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("InstanceId")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<string>("LockId")
                        .IsConcurrencyToken()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("LockedUntil")
                        .HasColumnType("datetime2");

                    b.Property<string>("Message")
                        .HasMaxLength(2147483647)
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Queue")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<string>("ReplyQueue")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

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
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(2147483647)
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ExecutionId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("InstanceId")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

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
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CompletedTime")
                        .HasColumnType("datetime2");

                    b.Property<long>("CompressedSize")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("CustomStatus")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Input")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("InstanceId")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<DateTime>("LastUpdatedTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<string>("Output")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ParentInstance")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<long>("Size")
                        .HasColumnType("bigint");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("ExecutionId");

                    b.ToTable("Executions");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Instance", b =>
                {
                    b.Property<string>("InstanceId")
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<string>("LastExecutionId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("LastQueue")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<string>("LockId")
                        .IsConcurrencyToken()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("LockedUntil")
                        .HasColumnType("datetime2");

                    b.HasKey("InstanceId");

                    b.HasIndex("LastExecutionId");

                    b.ToTable("Instances");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.OrchestrationMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(36)
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("AvailableAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ExecutionId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("InstanceId")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<string>("Message")
                        .HasMaxLength(2147483647)
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Queue")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("SequenceNumber")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("InstanceId");

                    b.HasIndex("AvailableAt", "Queue", "InstanceId");

                    b.ToTable("OrchestrationMessages");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.ActivityMessage", b =>
                {
                    b.HasOne("LLL.DurableTask.EFCore.Entities.Instance", "Instance")
                        .WithMany()
                        .HasForeignKey("InstanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Instance");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Event", b =>
                {
                    b.HasOne("LLL.DurableTask.EFCore.Entities.Execution", "Execution")
                        .WithMany()
                        .HasForeignKey("ExecutionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Execution");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Execution", b =>
                {
                    b.OwnsMany("LLL.DurableTask.EFCore.Entities.Tag", "Tags", b1 =>
                        {
                            b1.Property<string>("ExecutionId")
                                .HasColumnType("nvarchar(100)");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("Id"), 1L, 1);

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(2000)
                                .HasColumnType("nvarchar(2000)");

                            b1.HasKey("ExecutionId", "Id");

                            b1.ToTable("ExecutionTags", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("ExecutionId");
                        });

                    b.Navigation("Tags");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.Instance", b =>
                {
                    b.HasOne("LLL.DurableTask.EFCore.Entities.Execution", "LastExecution")
                        .WithMany()
                        .HasForeignKey("LastExecutionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LastExecution");
                });

            modelBuilder.Entity("LLL.DurableTask.EFCore.Entities.OrchestrationMessage", b =>
                {
                    b.HasOne("LLL.DurableTask.EFCore.Entities.Instance", "Instance")
                        .WithMany()
                        .HasForeignKey("InstanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Instance");
                });
#pragma warning restore 612, 618
        }
    }
}