using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashZone.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingAddressToProductRedeem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingCity",
                table: "ProductRedeems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCountry",
                table: "ProductRedeems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingFullName",
                table: "ProductRedeems",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPhone",
                table: "ProductRedeems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPostalCode",
                table: "ProductRedeems",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingStreet",
                table: "ProductRedeems",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingCity",
                table: "ProductRedeems");

            migrationBuilder.DropColumn(
                name: "ShippingCountry",
                table: "ProductRedeems");

            migrationBuilder.DropColumn(
                name: "ShippingFullName",
                table: "ProductRedeems");

            migrationBuilder.DropColumn(
                name: "ShippingPhone",
                table: "ProductRedeems");

            migrationBuilder.DropColumn(
                name: "ShippingPostalCode",
                table: "ProductRedeems");

            migrationBuilder.DropColumn(
                name: "ShippingStreet",
                table: "ProductRedeems");
        }
    }
}
