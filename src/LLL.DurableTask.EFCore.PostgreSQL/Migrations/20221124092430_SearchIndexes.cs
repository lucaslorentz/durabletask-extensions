using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLL.DurableTask.EFCore.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class SearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Executions_LastExecutionId",
                table: "Instances");

            migrationBuilder.DropIndex(
                name: "IX_Instances_LastExecutionId",
                table: "Instances");

            migrationBuilder.CreateIndex(
                name: "IX_Instances_LastExecutionId",
                table: "Instances",
                column: "LastExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_CreatedTime",
                table: "Executions",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_CreatedTime_InstanceId",
                table: "Executions",
                columns: new[] { "CreatedTime", "InstanceId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_InstanceId",
                table: "Executions",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_Name",
                table: "Executions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_Status",
                table: "Executions",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Executions_LastExecutionId",
                table: "Instances",
                column: "LastExecutionId",
                principalTable: "Executions",
                principalColumn: "ExecutionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instances_Executions_LastExecutionId",
                table: "Instances");
                
            migrationBuilder.DropIndex(
                name: "IX_Instances_LastExecutionId",
                table: "Instances");

            migrationBuilder.DropIndex(
                name: "IX_Executions_CreatedTime",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_Executions_CreatedTime_InstanceId",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_Executions_InstanceId",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_Executions_Name",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_Executions_Status",
                table: "Executions");

            migrationBuilder.CreateIndex(
                name: "IX_Instances_LastExecutionId",
                table: "Instances",
                column: "LastExecutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Instances_Executions_LastExecutionId",
                table: "Instances",
                column: "LastExecutionId",
                principalTable: "Executions",
                principalColumn: "ExecutionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
