using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoWebappAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotographerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Concepts",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Concepts",
                table: "AspNetUsers");
        }
    }
}
