using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Nomenclature.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class InitialNomenclature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "nomenclature");

            migrationBuilder.CreateTable(
                name: "Countries",
                schema: "nomenclature",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Iso2Code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Iso3Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhonePrefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "nomenclature",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StateProvinces",
                schema: "nomenclature",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateProvinces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateProvinces_Countries_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "nomenclature",
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                schema: "nomenclature",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateProvinceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_StateProvinces_StateProvinceId",
                        column: x => x.StateProvinceId,
                        principalSchema: "nomenclature",
                        principalTable: "StateProvinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_StateProvinceId",
                schema: "nomenclature",
                table: "Cities",
                column: "StateProvinceId");

            migrationBuilder.CreateIndex(
                name: "UQ_Cities_StateProvinceId_Name",
                schema: "nomenclature",
                table: "Cities",
                columns: new[] { "StateProvinceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Name",
                schema: "nomenclature",
                table: "Countries",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "UQ_Countries_Iso2Code",
                schema: "nomenclature",
                table: "Countries",
                column: "Iso2Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Countries_Iso3Code",
                schema: "nomenclature",
                table: "Countries",
                column: "Iso3Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Currencies_Code",
                schema: "nomenclature",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateProvinces_CountryId",
                schema: "nomenclature",
                table: "StateProvinces",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "UQ_StateProvinces_CountryId_Code",
                schema: "nomenclature",
                table: "StateProvinces",
                columns: new[] { "CountryId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cities",
                schema: "nomenclature");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "nomenclature");

            migrationBuilder.DropTable(
                name: "StateProvinces",
                schema: "nomenclature");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "nomenclature");
        }
    }
}
