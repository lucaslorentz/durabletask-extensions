using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLL.DurableTask.EFCore.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AdjustIndexesForLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityMessages_LockedUntil",
                table: "ActivityMessages");

            migrationBuilder.CreateIndex(
                name: "IX_Instances_InstanceId_LockedUntil",
                table: "Instances",
                columns: new[] { "InstanceId", "LockedUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Instances_InstanceId_LockedUntil",
                table: "Instances");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMessages_LockedUntil",
                table: "ActivityMessages",
                column: "LockedUntil");
        }
    }
}
