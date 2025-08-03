using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashZone.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MakingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Tournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "Tournaments");
        }
    }
}
