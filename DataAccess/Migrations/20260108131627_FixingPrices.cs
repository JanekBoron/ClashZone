using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashZone.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixingPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Prize",
                table: "Tournaments",
                newName: "ThirdPlacePrize");

            migrationBuilder.AddColumn<string>(
                name: "FirstPlacePrize",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecondPlacePrize",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstPlacePrize",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "SecondPlacePrize",
                table: "Tournaments");

            migrationBuilder.RenameColumn(
                name: "ThirdPlacePrize",
                table: "Tournaments",
                newName: "Prize");
        }
    }
}
