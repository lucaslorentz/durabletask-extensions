using Microsoft.EntityFrameworkCore.Migrations;

namespace LLL.DurableTask.EFCore.MySql.Migrations
{
    public partial class FKEventToExecution : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Events_Executions_InstanceId_ExecutionId",
                table: "Events",
                columns: new[] { "InstanceId", "ExecutionId" },
                principalTable: "Executions",
                principalColumns: new[] { "InstanceId", "ExecutionId" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Executions_InstanceId_ExecutionId",
                table: "Events");
        }
    }
}
