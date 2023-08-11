using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLL.DurableTask.EFCore.MySql.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "Executions",
            columns: table => new
            {
                ExecutionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                InstanceId = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Name = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Version = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CompletedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LastUpdatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CompressedSize = table.Column<long>(type: "bigint", nullable: false),
                Size = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CustomStatus = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ParentInstance = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Input = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Output = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Executions", x => x.ExecutionId);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                InstanceId = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ExecutionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                SequenceNumber = table.Column<int>(type: "int", nullable: false),
                Content = table.Column<string>(type: "longtext", maxLength: 2147483647, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
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
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "ExecutionTags",
            columns: table => new
            {
                ExecutionId = table.Column<string>(type: "varchar(100)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Value = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
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
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "Instances",
            columns: table => new
            {
                InstanceId = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                LastExecutionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                LastQueue = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                LockedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LockId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
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
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "ActivityMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                InstanceId = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Queue = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ReplyQueue = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Message = table.Column<string>(type: "longtext", maxLength: 2147483647, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LockedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LockId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
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
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "OrchestrationMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", maxLength: 36, nullable: false, collation: "ascii_general_ci"),
                InstanceId = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ExecutionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Queue = table.Column<string>(type: "varchar(255)", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                AvailableAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                SequenceNumber = table.Column<int>(type: "int", nullable: false),
                Message = table.Column<string>(type: "longtext", maxLength: 2147483647, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrchestrationMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrchestrationMessages_Instances_InstanceId",
                    column: x => x.InstanceId,
                    principalTable: "Instances",
                    principalColumn: "InstanceId",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

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
            name: "IX_OrchestrationMessages_AvailableAt_Queue_InstanceId",
            table: "OrchestrationMessages",
            columns: new[] { "AvailableAt", "Queue", "InstanceId" });

        migrationBuilder.CreateIndex(
            name: "IX_OrchestrationMessages_InstanceId",
            table: "OrchestrationMessages",
            column: "InstanceId");
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
            name: "Instances");

        migrationBuilder.DropTable(
            name: "Executions");
    }
}
