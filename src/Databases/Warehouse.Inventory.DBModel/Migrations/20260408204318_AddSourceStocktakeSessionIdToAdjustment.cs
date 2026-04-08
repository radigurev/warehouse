using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Inventory.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceStocktakeSessionIdToAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceStocktakeSessionId",
                schema: "inventory",
                table: "InventoryAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_SourceStocktakeSessionId",
                schema: "inventory",
                table: "InventoryAdjustments",
                column: "SourceStocktakeSessionId",
                filter: "[SourceStocktakeSessionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_StocktakeSessions_SourceStocktakeSessionId",
                schema: "inventory",
                table: "InventoryAdjustments",
                column: "SourceStocktakeSessionId",
                principalSchema: "inventory",
                principalTable: "StocktakeSessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_StocktakeSessions_SourceStocktakeSessionId",
                schema: "inventory",
                table: "InventoryAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAdjustments_SourceStocktakeSessionId",
                schema: "inventory",
                table: "InventoryAdjustments");

            migrationBuilder.DropColumn(
                name: "SourceStocktakeSessionId",
                schema: "inventory",
                table: "InventoryAdjustments");
        }
    }
}
