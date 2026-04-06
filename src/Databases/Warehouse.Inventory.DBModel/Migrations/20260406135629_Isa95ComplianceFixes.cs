using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Inventory.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class Isa95ComplianceFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresBatchTracking",
                schema: "inventory",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresBatchTracking",
                schema: "inventory",
                table: "Products");
        }
    }
}
