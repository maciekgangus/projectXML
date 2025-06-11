using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XMLApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTreeDataToXml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TreeName",
                table: "OrganizationTrees",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TreeData",
                table: "OrganizationTrees",
                type: "xml",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "xml");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TreeName",
                table: "OrganizationTrees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TreeData",
                table: "OrganizationTrees",
                type: "xml",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "xml",
                oldNullable: true);
        }
    }
}
