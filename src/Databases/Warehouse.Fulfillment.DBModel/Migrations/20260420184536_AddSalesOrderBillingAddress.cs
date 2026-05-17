using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Fulfillment.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrderBillingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                schema: "fulfillment",
                table: "SalesOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingCountryCode",
                schema: "fulfillment",
                table: "SalesOrders",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingPostalCode",
                schema: "fulfillment",
                table: "SalesOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingStateProvince",
                schema: "fulfillment",
                table: "SalesOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingStreetLine1",
                schema: "fulfillment",
                table: "SalesOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingStreetLine2",
                schema: "fulfillment",
                table: "SalesOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Backfill: default billing address to the shipping address for pre-existing sales orders.
            migrationBuilder.Sql(@"
                UPDATE fulfillment.SalesOrders
                SET BillingStreetLine1   = ShippingStreetLine1,
                    BillingStreetLine2   = ShippingStreetLine2,
                    BillingCity          = ShippingCity,
                    BillingStateProvince = ShippingStateProvince,
                    BillingPostalCode    = ShippingPostalCode,
                    BillingCountryCode   = ShippingCountryCode
                WHERE BillingStreetLine1 = ''
                   OR BillingCity = ''
                   OR BillingPostalCode = ''
                   OR BillingCountryCode = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingCity",
                schema: "fulfillment",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "BillingCountryCode",
                schema: "fulfillment",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "BillingPostalCode",
                schema: "fulfillment",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "BillingStateProvince",
                schema: "fulfillment",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "BillingStreetLine1",
                schema: "fulfillment",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "BillingStreetLine2",
                schema: "fulfillment",
                table: "SalesOrders");
        }
    }
}
