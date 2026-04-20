using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Fulfillment.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPricesAndSoCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                schema: "fulfillment",
                table: "SalesOrders",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CustomerAccountId",
                schema: "fulfillment",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE so
                SET so.CustomerAccountId = ca.Id,
                    so.CurrencyCode = ca.CurrencyCode
                FROM fulfillment.SalesOrders so
                CROSS APPLY (
                    SELECT TOP 1 Id, CurrencyCode
                    FROM customers.CustomerAccounts
                    WHERE CustomerId = so.CustomerId
                      AND IsPrimary = 1
                      AND IsDeleted = 0
                    ORDER BY Id
                ) ca
                WHERE so.CustomerAccountId = 0 OR so.CurrencyCode = '';
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM fulfillment.SalesOrders
                    WHERE CustomerAccountId = 0 OR CurrencyCode = ''
                )
                BEGIN
                    DECLARE @missing INT = (
                        SELECT COUNT(*) FROM fulfillment.SalesOrders
                        WHERE CustomerAccountId = 0 OR CurrencyCode = ''
                    );
                    DECLARE @msg NVARCHAR(400) = CONCAT(
                        'CHG-FEAT-007 backfill failed: ', @missing,
                        ' sales order row(s) have no matching primary CustomerAccount. ',
                        'Backfill these rows manually (set CustomerAccountId + CurrencyCode from a valid customers.CustomerAccounts row) before re-running this migration.'
                    );
                    THROW 50000, @msg, 1;
                END;
            ");

            migrationBuilder.CreateTable(
                name: "ProductPrices",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPrices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerAccountId",
                schema: "fulfillment",
                table: "SalesOrders",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_ProductId_CurrencyCode_ValidFrom_ValidTo",
                schema: "fulfillment",
                table: "ProductPrices",
                columns: new[] { "ProductId", "CurrencyCode", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "UX_ProductPrices_ProductId_CurrencyCode_ValidFrom",
                schema: "fulfillment",
                table: "ProductPrices",
                columns: new[] { "ProductId", "CurrencyCode", "ValidFrom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPrices",
                schema: "fulfillment");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CustomerAccountId",
                schema: "fulfillment",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                schema: "fulfillment",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CustomerAccountId",
                schema: "fulfillment",
                table: "SalesOrders");
        }
    }
}
