using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Purchasing.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class InitialPurchasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "purchasing");

            migrationBuilder.CreateTable(
                name: "PurchaseEvents",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierCategories",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_SupplierCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "purchasing",
                        principalTable: "SupplierCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    DestinationWarehouseId = table.Column<int>(type: "int", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierAddresses",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StreetLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StreetLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierAddresses_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierEmails",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    EmailType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierEmails_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPhones",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    PhoneType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPhones_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturns",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsReceiptId = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderLineId = table.Column<int>(type: "int", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ManufacturingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InspectionStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    InspectionNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InspectedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    InspectedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierReturnId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    GoodsReceiptLineId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_SupplierReturns_SupplierReturnId",
                        column: x => x.SupplierReturnId,
                        principalSchema: "purchasing",
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_GoodsReceiptId",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_PurchaseOrderLineId",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ReceiptNumber",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ReceivedAtUtc",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_WarehouseId",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseEvents_EntityType_EntityId",
                schema: "purchasing",
                table: "PurchaseEvents",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseEvents_EventType",
                schema: "purchasing",
                table: "PurchaseEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseEvents_OccurredAtUtc",
                schema: "purchasing",
                table: "PurchaseEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_POId_ProductId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                columns: new[] { "PurchaseOrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_ProductId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedAtUtc",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_DestinationWarehouseId",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAddresses_SupplierId",
                schema: "purchasing",
                table: "SupplierAddresses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCategories_Name",
                schema: "purchasing",
                table: "SupplierCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEmails_SupplierId",
                schema: "purchasing",
                table: "SupplierEmails",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEmails_SupplierId_EmailAddress",
                schema: "purchasing",
                table: "SupplierEmails",
                columns: new[] { "SupplierId", "EmailAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPhones_SupplierId",
                schema: "purchasing",
                table: "SupplierPhones",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_GoodsReceiptLineId",
                schema: "purchasing",
                table: "SupplierReturnLines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_SupplierReturnId",
                schema: "purchasing",
                table: "SupplierReturnLines",
                column: "SupplierReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_ReturnNumber",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_Status",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_SupplierId",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CategoryId",
                schema: "purchasing",
                table: "Suppliers",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                schema: "purchasing",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsDeleted",
                schema: "purchasing",
                table: "Suppliers",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                schema: "purchasing",
                table: "Suppliers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TaxId",
                schema: "purchasing",
                table: "Suppliers",
                column: "TaxId",
                filter: "[IsDeleted] = 0 AND [TaxId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseEvents",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierAddresses",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierEmails",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierPhones",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierReturnLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierReturns",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "GoodsReceipts",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierCategories",
                schema: "purchasing");
        }
    }
}
