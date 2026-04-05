using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Customers.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerNativeLanguageName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NativeLanguageName",
                schema: "customers",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NativeLanguageName",
                schema: "customers",
                table: "Customers");
        }
    }
}
