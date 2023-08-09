using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLL.DurableTask.EFCore.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class IndexCompletedTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Executions_CompletedTime",
                table: "Executions",
                column: "CompletedTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Executions_CompletedTime",
                table: "Executions");
        }
    }
}
