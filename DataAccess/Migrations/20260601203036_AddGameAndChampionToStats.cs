using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashZone.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddGameAndChampionToStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Game",
                table: "UserStats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Counter Strike 2");

            migrationBuilder.AddColumn<string>(
                name: "Champion",
                table: "PlayerMatchStats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Game",
                table: "PlayerMatchStats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Counter Strike 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Game",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "Champion",
                table: "PlayerMatchStats");

            migrationBuilder.DropColumn(
                name: "Game",
                table: "PlayerMatchStats");
        }
    }
}
