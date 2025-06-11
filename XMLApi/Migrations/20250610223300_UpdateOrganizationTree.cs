using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XMLApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrganizationTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationTrees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TreeData = table.Column<string>(type: "xml", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationTrees", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationTrees");
        }
    }
}
