using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Fulfillment.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class InitialFulfillment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fulfillment");

            migrationBuilder.CreateTable(
                name: "Carriers",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrackingUrlTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FulfillmentEvents",
                schema: "fulfillment",
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
                    table.PrimaryKey("PK_FulfillmentEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarrierServiceLevels",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EstimatedDeliveryDays = table.Column<int>(type: "int", nullable: true),
                    BaseRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PerKgRate = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierServiceLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierServiceLevels_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "fulfillment",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    RequestedShipDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ShippingStreetLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShippingStreetLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShippingCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShippingStateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShippingPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShippingCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CarrierId = table.Column<int>(type: "int", nullable: true),
                    CarrierServiceLevelId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: true),
                    ShippedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CompletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_CarrierServiceLevels_CarrierServiceLevelId",
                        column: x => x.CarrierServiceLevelId,
                        principalSchema: "fulfillment",
                        principalTable: "CarrierServiceLevels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "fulfillment",
                        principalTable: "Carriers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturns",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SalesOrderId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturns_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "fulfillment",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Parcels",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcelNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    Length = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Width = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parcels_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "fulfillment",
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PickLists",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickListNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickLists_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "fulfillment",
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderLines",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PickedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    PackedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    ShippedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "fulfillment",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    CarrierId = table.Column<int>(type: "int", nullable: true),
                    CarrierServiceLevelId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Dispatched"),
                    ShippingStreetLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShippingStreetLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShippingCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShippingStateProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShippingPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShippingCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrackingUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    DispatchedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_CarrierServiceLevels_CarrierServiceLevelId",
                        column: x => x.CarrierServiceLevelId,
                        principalSchema: "fulfillment",
                        principalTable: "CarrierServiceLevels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Shipments_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "fulfillment",
                        principalTable: "Carriers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Shipments_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "fulfillment",
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturnLines",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerReturnId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnLines_CustomerReturns_CustomerReturnId",
                        column: x => x.CustomerReturnId,
                        principalSchema: "fulfillment",
                        principalTable: "CustomerReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickListLines",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickListId = table.Column<int>(type: "int", nullable: false),
                    SalesOrderLineId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    PickedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    PickedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickListLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickListLines_PickLists_PickListId",
                        column: x => x.PickListId,
                        principalSchema: "fulfillment",
                        principalTable: "PickLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickListLines_SalesOrderLines_SalesOrderLineId",
                        column: x => x.SalesOrderLineId,
                        principalSchema: "fulfillment",
                        principalTable: "SalesOrderLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShipmentLines",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    SalesOrderLineId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    BatchId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentLines_SalesOrderLines_SalesOrderLineId",
                        column: x => x.SalesOrderLineId,
                        principalSchema: "fulfillment",
                        principalTable: "SalesOrderLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShipmentLines_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "fulfillment",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentTrackingEntries",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentTrackingEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentTrackingEntries_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "fulfillment",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParcelItems",
                schema: "fulfillment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcelId = table.Column<int>(type: "int", nullable: false),
                    PickListLineId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParcelItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParcelItems_Parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "fulfillment",
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParcelItems_PickListLines_PickListLineId",
                        column: x => x.PickListLineId,
                        principalSchema: "fulfillment",
                        principalTable: "PickListLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_Code",
                schema: "fulfillment",
                table: "Carriers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_Name",
                schema: "fulfillment",
                table: "Carriers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierServiceLevels_CarrierId",
                schema: "fulfillment",
                table: "CarrierServiceLevels",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierServiceLevels_CarrierId_Code",
                schema: "fulfillment",
                table: "CarrierServiceLevels",
                columns: new[] { "CarrierId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnLines_CustomerReturnId",
                schema: "fulfillment",
                table: "CustomerReturnLines",
                column: "CustomerReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_CreatedAtUtc",
                schema: "fulfillment",
                table: "CustomerReturns",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_CustomerId",
                schema: "fulfillment",
                table: "CustomerReturns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_ReturnNumber",
                schema: "fulfillment",
                table: "CustomerReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_SalesOrderId",
                schema: "fulfillment",
                table: "CustomerReturns",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_Status",
                schema: "fulfillment",
                table: "CustomerReturns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentEvents_EntityType_EntityId",
                schema: "fulfillment",
                table: "FulfillmentEvents",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentEvents_EventType",
                schema: "fulfillment",
                table: "FulfillmentEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentEvents_OccurredAtUtc",
                schema: "fulfillment",
                table: "FulfillmentEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelItems_ParcelId",
                schema: "fulfillment",
                table: "ParcelItems",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelItems_PickListLineId",
                schema: "fulfillment",
                table: "ParcelItems",
                column: "PickListLineId");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_ParcelNumber",
                schema: "fulfillment",
                table: "Parcels",
                column: "ParcelNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_SalesOrderId",
                schema: "fulfillment",
                table: "Parcels",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListLines_PickListId",
                schema: "fulfillment",
                table: "PickListLines",
                column: "PickListId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListLines_ProductId",
                schema: "fulfillment",
                table: "PickListLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PickListLines_SalesOrderLineId",
                schema: "fulfillment",
                table: "PickListLines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PickLists_CreatedAtUtc",
                schema: "fulfillment",
                table: "PickLists",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PickLists_PickListNumber",
                schema: "fulfillment",
                table: "PickLists",
                column: "PickListNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickLists_SalesOrderId",
                schema: "fulfillment",
                table: "PickLists",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PickLists_Status",
                schema: "fulfillment",
                table: "PickLists",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_ProductId",
                schema: "fulfillment",
                table: "SalesOrderLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_SalesOrderId",
                schema: "fulfillment",
                table: "SalesOrderLines",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_SOId_ProductId",
                schema: "fulfillment",
                table: "SalesOrderLines",
                columns: new[] { "SalesOrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CarrierId",
                schema: "fulfillment",
                table: "SalesOrders",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CarrierServiceLevelId",
                schema: "fulfillment",
                table: "SalesOrders",
                column: "CarrierServiceLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CreatedAtUtc",
                schema: "fulfillment",
                table: "SalesOrders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                schema: "fulfillment",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OrderNumber",
                schema: "fulfillment",
                table: "SalesOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Status",
                schema: "fulfillment",
                table: "SalesOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_WarehouseId",
                schema: "fulfillment",
                table: "SalesOrders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLines_SalesOrderLineId",
                schema: "fulfillment",
                table: "ShipmentLines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLines_ShipmentId",
                schema: "fulfillment",
                table: "ShipmentLines",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CarrierId",
                schema: "fulfillment",
                table: "Shipments",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CarrierServiceLevelId",
                schema: "fulfillment",
                table: "Shipments",
                column: "CarrierServiceLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DispatchedAtUtc",
                schema: "fulfillment",
                table: "Shipments",
                column: "DispatchedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_SalesOrderId",
                schema: "fulfillment",
                table: "Shipments",
                column: "SalesOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ShipmentNumber",
                schema: "fulfillment",
                table: "Shipments",
                column: "ShipmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status",
                schema: "fulfillment",
                table: "Shipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentTrackingEntries_ShipmentId",
                schema: "fulfillment",
                table: "ShipmentTrackingEntries",
                column: "ShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerReturnLines",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "FulfillmentEvents",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "ParcelItems",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "ShipmentLines",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "ShipmentTrackingEntries",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "CustomerReturns",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "Parcels",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "PickListLines",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "Shipments",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "PickLists",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "SalesOrderLines",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "SalesOrders",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "CarrierServiceLevels",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "Carriers",
                schema: "fulfillment");
        }
    }
}
