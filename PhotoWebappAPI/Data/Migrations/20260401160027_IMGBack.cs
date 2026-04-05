using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoWebappAPI.Migrations
{
    /// <inheritdoc />
    public partial class IMGBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GalleryPassword",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GalleryPassword",
                table: "Bookings");
        }
    }
}
