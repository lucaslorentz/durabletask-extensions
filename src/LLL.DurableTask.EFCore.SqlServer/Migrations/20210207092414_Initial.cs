using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LLL.DurableTask.EFCore.SqlServer.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    ExecutionId = table.Column<string>(maxLength: 100, nullable: false),
                    InstanceId = table.Column<string>(maxLength: 250, nullable: false),
                    Name = table.Column<string>(maxLength: 250, nullable: false),
                    Version = table.Column<string>(maxLength: 100, nullable: false),
                    CreatedTime = table.Column<DateTime>(nullable: false),
                    CompletedTime = table.Column<DateTime>(nullable: false),
                    LastUpdatedTime = table.Column<DateTime>(nullable: false),
                    CompressedSize = table.Column<long>(nullable: false),
                    Size = table.Column<long>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    CustomStatus = table.Column<string>(nullable: true),
                    ParentInstance = table.Column<string>(maxLength: 2000, nullable: true),
                    Input = table.Column<string>(nullable: true),
                    Output = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.ExecutionId);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    InstanceId = table.Column<string>(maxLength: 250, nullable: false),
                    ExecutionId = table.Column<string>(maxLength: 100, nullable: false),
                    SequenceNumber = table.Column<int>(nullable: false),
                    Content = table.Column<string>(maxLength: 2147483647, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Executions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "Executions",
                        principalColumn: "ExecutionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionTags",
                columns: table => new
                {
                    ExecutionId = table.Column<string>(nullable: false),
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Value = table.Column<string>(maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionTags", x => new { x.ExecutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExecutionTags_Executions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "Executions",
                        principalColumn: "ExecutionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Instances",
                columns: table => new
                {
                    InstanceId = table.Column<string>(maxLength: 250, nullable: false),
                    LastExecutionId = table.Column<string>(maxLength: 100, nullable: false),
                    LastQueueName = table.Column<string>(maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instances", x => x.InstanceId);
                    table.ForeignKey(
                        name: "FK_Instances_Executions_LastExecutionId",
                        column: x => x.LastExecutionId,
                        principalTable: "Executions",
                        principalColumn: "ExecutionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    InstanceId = table.Column<string>(maxLength: 250, nullable: false),
                    Queue = table.Column<string>(maxLength: 250, nullable: false),
                    ReplyQueue = table.Column<string>(maxLength: 250, nullable: false),
                    Message = table.Column<string>(maxLength: 2147483647, nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    LockedUntil = table.Column<DateTime>(nullable: false),
                    LockId = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityMessages_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "InstanceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrchestrationBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(maxLength: 36, nullable: false),
                    InstanceId = table.Column<string>(maxLength: 250, nullable: false),
                    Queue = table.Column<string>(maxLength: 250, nullable: false),
                    AvailableAt = table.Column<DateTime>(nullable: false),
                    LockedUntil = table.Column<DateTime>(nullable: false),
                    LockId = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrchestrationBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrchestrationBatches_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "InstanceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrchestrationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(maxLength: 36, nullable: false),
                    BatchId = table.Column<Guid>(maxLength: 36, nullable: false),
                    ExecutionId = table.Column<string>(maxLength: 100, nullable: true),
                    AvailableAt = table.Column<DateTime>(nullable: false),
                    SequenceNumber = table.Column<int>(nullable: false),
                    Message = table.Column<string>(maxLength: 2147483647, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrchestrationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrchestrationMessages_OrchestrationBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "OrchestrationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMessages_InstanceId",
                table: "ActivityMessages",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMessages_LockedUntil",
                table: "ActivityMessages",
                column: "LockedUntil");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMessages_LockedUntil_Queue",
                table: "ActivityMessages",
                columns: new[] { "LockedUntil", "Queue" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ExecutionId",
                table: "Events",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_InstanceId_ExecutionId_SequenceNumber",
                table: "Events",
                columns: new[] { "InstanceId", "ExecutionId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instances_LastExecutionId",
                table: "Instances",
                column: "LastExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationBatches_InstanceId_Queue",
                table: "OrchestrationBatches",
                columns: new[] { "InstanceId", "Queue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationBatches_AvailableAt_LockedUntil_Queue",
                table: "OrchestrationBatches",
                columns: new[] { "AvailableAt", "LockedUntil", "Queue" });

            migrationBuilder.CreateIndex(
                name: "IX_OrchestrationMessages_BatchId",
                table: "OrchestrationMessages",
                column: "BatchId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityMessages");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "ExecutionTags");

            migrationBuilder.DropTable(
                name: "OrchestrationMessages");

            migrationBuilder.DropTable(
                name: "OrchestrationBatches");

            migrationBuilder.DropTable(
                name: "Instances");

            migrationBuilder.DropTable(
                name: "Executions");
        }
    }
}
