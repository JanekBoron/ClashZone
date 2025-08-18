using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashZone.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ReportFunctionality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReport",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReport",
                table: "ChatMessages");
        }
    }
}
