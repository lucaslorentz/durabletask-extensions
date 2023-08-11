using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLL.DurableTask.EFCore.SqlServer.Migrations;

/// <inheritdoc />
public partial class IndexTags : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Value",
            table: "ExecutionTags",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(2000)",
            oldMaxLength: 2000);

        migrationBuilder.CreateIndex(
            name: "IX_ExecutionTags_ExecutionId_Name",
            table: "ExecutionTags",
            columns: new[] { "ExecutionId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExecutionTags_Name_Value",
            table: "ExecutionTags",
            columns: new[] { "Name", "Value" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ExecutionTags_ExecutionId_Name",
            table: "ExecutionTags");

        migrationBuilder.DropIndex(
            name: "IX_ExecutionTags_Name_Value",
            table: "ExecutionTags");

        migrationBuilder.AlterColumn<string>(
            name: "Value",
            table: "ExecutionTags",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(500)",
            oldMaxLength: 500);
    }
}
