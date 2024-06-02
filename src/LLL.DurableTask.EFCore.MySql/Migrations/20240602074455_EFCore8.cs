using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLL.DurableTask.EFCore.MySql.Migrations;

/// <inheritdoc />
public partial class EFCore8 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Id",
            table: "ExecutionTags",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int")
            .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "Id",
            table: "ExecutionTags",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int")
            .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
    }
}
