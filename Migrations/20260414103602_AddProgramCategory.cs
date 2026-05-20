using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyalo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Programs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Programs");
        }
    }
}
