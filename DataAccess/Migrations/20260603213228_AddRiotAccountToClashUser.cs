using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashZone.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRiotAccountToClashUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RiotId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiotPuuid",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RiotId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RiotPuuid",
                table: "AspNetUsers");
        }
    }
}
