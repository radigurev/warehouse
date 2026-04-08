using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Inventory.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedProductCategories(migrationBuilder);
            SeedUnitsOfMeasure(migrationBuilder);
            SeedProducts(migrationBuilder);
            SeedWarehouses(migrationBuilder);
            SeedZones(migrationBuilder);
            SeedStorageLocations(migrationBuilder);
            SeedBatches(migrationBuilder);
            SeedStockLevels(migrationBuilder);
            SeedStockMovements(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [inventory].[StockMovements] WHERE [Id] BETWEEN 1 AND 30;
DELETE FROM [inventory].[StockLevels] WHERE [Id] BETWEEN 1 AND 40;
DELETE FROM [inventory].[Batches] WHERE [Id] BETWEEN 1 AND 10;
DELETE FROM [inventory].[StorageLocations] WHERE [Id] BETWEEN 1 AND 24;
DELETE FROM [inventory].[Zones] WHERE [Id] BETWEEN 1 AND 8;
DELETE FROM [inventory].[Warehouses] WHERE [Id] BETWEEN 1 AND 3;
DELETE FROM [inventory].[Products] WHERE [Id] BETWEEN 1 AND 25;
DELETE FROM [inventory].[UnitsOfMeasure] WHERE [Id] BETWEEN 1 AND 8;
DELETE FROM [inventory].[ProductCategories] WHERE [Id] BETWEEN 1 AND 6;
");
        }

        private static void SeedProductCategories(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[ProductCategories] ON;

IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductCategories] WHERE [Id] = 1)
    INSERT INTO [inventory].[ProductCategories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAtUtc])
    VALUES (1, N'Raw Materials', N'Unprocessed materials used in manufacturing', NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductCategories] WHERE [Id] = 2)
    INSERT INTO [inventory].[ProductCategories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAtUtc])
    VALUES (2, N'Finished Goods', N'Completed products ready for sale or distribution', NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductCategories] WHERE [Id] = 3)
    INSERT INTO [inventory].[ProductCategories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAtUtc])
    VALUES (3, N'Packaging', N'Packaging materials — boxes, wrap, pallets', NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductCategories] WHERE [Id] = 4)
    INSERT INTO [inventory].[ProductCategories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAtUtc])
    VALUES (4, N'Spare Parts', N'Replacement parts for equipment and machinery', NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductCategories] WHERE [Id] = 5)
    INSERT INTO [inventory].[ProductCategories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAtUtc])
    VALUES (5, N'Chemicals', N'Industrial chemicals and solvents', 1, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductCategories] WHERE [Id] = 6)
    INSERT INTO [inventory].[ProductCategories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAtUtc])
    VALUES (6, N'Electronics', N'Electronic components and assembled units', 2, '2026-04-01T00:00:00');

SET IDENTITY_INSERT [inventory].[ProductCategories] OFF;
");
        }

        private static void SeedUnitsOfMeasure(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[UnitsOfMeasure] ON;

IF NOT EXISTS (SELECT 1 FROM [inventory].[UnitsOfMeasure] WHERE [Id] = 1)
    INSERT INTO [inventory].[UnitsOfMeasure] ([Id], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (1, N'PCS', N'Pieces', N'Individual units or items', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[UnitsOfMeasure] WHERE [Id] = 2)
    INSERT INTO [inventory].[UnitsOfMeasure] ([Id], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (2, N'KG', N'Kilograms', N'Weight in kilograms', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[UnitsOfMeasure] WHERE [Id] = 3)
    INSERT INTO [inventory].[UnitsOfMeasure] ([Id], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (3, N'L', N'Litres', N'Volume in litres', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[UnitsOfMeasure] WHERE [Id] = 4)
    INSERT INTO [inventory].[UnitsOfMeasure] ([Id], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (4, N'M', N'Metres', N'Length in metres', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[UnitsOfMeasure] WHERE [Id] = 5)
    INSERT INTO [inventory].[UnitsOfMeasure] ([Id], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (5, N'BOX', N'Boxes', N'Standard shipping boxes', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[UnitsOfMeasure] WHERE [Id] = 6)
    INSERT INTO [inventory].[UnitsOfMeasure] ([Id], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (6, N'PAL', N'Pallets', N'Standard EUR pallets (120x80cm)', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[UnitsOfMeasure] WHERE [Id] = 7)
    INSERT INTO [inventory].[UnitsOfMeasure] ([Id], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (7, N'M2', N'Square Metres', N'Area in square metres', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[UnitsOfMeasure] WHERE [Id] = 8)
    INSERT INTO [inventory].[UnitsOfMeasure] ([Id], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (8, N'ROLL', N'Rolls', N'Rolled material (film, tape, fabric)', '2026-04-01T00:00:00');

SET IDENTITY_INSERT [inventory].[UnitsOfMeasure] OFF;
");
        }

        private static void SeedProducts(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[Products] ON;

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 1)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (1, N'PROD-001', N'Steel Sheet 2mm', N'Cold-rolled steel sheet, 2mm thickness, 1000x2000mm', N'STL-SHT-2MM', N'5901234567890', 1, 1, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 2)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (2, N'PROD-002', N'Aluminium Rod 10mm', N'Aluminium alloy rod, 10mm diameter, 3m length', N'ALU-ROD-10', N'5901234567891', 1, 4, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 3)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (3, N'PROD-003', N'Industrial Solvent A', N'Multi-purpose industrial cleaning solvent, 5L can', N'CHM-SOL-A5L', N'5901234567892', 5, 3, 1, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 4)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (4, N'PROD-004', N'Epoxy Resin 2K', N'Two-component epoxy resin system, 1kg kit', N'CHM-EPX-2K1', N'5901234567893', 5, 2, 1, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 5)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (5, N'PROD-005', N'Circuit Board PCB-X1', N'Main controller PCB, 4-layer, assembled', N'ELC-PCB-X1', N'5901234567894', 6, 1, 1, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 6)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (6, N'PROD-006', N'Power Supply Unit 24V', N'Industrial PSU, 24V DC, 10A output', N'ELC-PSU-24V', N'5901234567895', 6, 1, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 7)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (7, N'PROD-007', N'Corrugated Box 400x300', N'Single-wall corrugated box, 400x300x200mm', N'PKG-BOX-4030', N'5901234567896', 3, 5, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 8)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (8, N'PROD-008', N'Stretch Wrap 500mm', N'Clear stretch film, 500mm width, 20 micron', N'PKG-WRP-500', N'5901234567897', 3, 8, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 9)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (9, N'PROD-009', N'EUR Pallet 1200x800', N'Standard EUR pallet, heat-treated', N'PKG-PAL-EUR', N'5901234567898', 3, 6, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 10)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (10, N'PROD-010', N'Bearing 6205-2RS', N'Deep groove ball bearing, sealed, 25x52x15mm', N'SPR-BRG-6205', N'5901234567899', 4, 1, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 11)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (11, N'PROD-011', N'V-Belt A68', N'Classical V-belt, A profile, 68 inch', N'SPR-VBT-A68', N'5901234567900', 4, 1, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 12)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (12, N'PROD-012', N'Hydraulic Oil ISO 46', N'Hydraulic fluid, ISO VG 46, 20L drum', N'CHM-HYD-46', N'5901234567901', 5, 3, 1, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 13)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (13, N'PROD-013', N'Copper Wire 1.5mm2', N'Insulated copper wire, 1.5mm2, 100m coil', N'RAW-COP-15', N'5901234567902', 1, 4, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 14)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (14, N'PROD-014', N'Stainless Bolt M8x30', N'Hex bolt, A2 stainless, M8x30mm, DIN 933', N'RAW-BLT-M830', N'5901234567903', 1, 1, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 15)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (15, N'PROD-015', N'Sensor Module SM-200', N'Industrial temperature sensor, 4-20mA output', N'ELC-SNS-200', N'5901234567904', 6, 1, 1, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 16)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (16, N'PROD-016', N'Rubber Gasket DN50', N'EPDM gasket, DN50 flange, PN16', N'SPR-GSK-DN50', N'5901234567905', 4, 1, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 17)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (17, N'PROD-017', N'Bubble Wrap 1200mm', N'Air bubble wrap, 1200mm width, large bubble', N'PKG-BWR-1200', N'5901234567906', 3, 8, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 18)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (18, N'PROD-018', N'Motor Drive VFD-3K', N'Variable frequency drive, 3kW, 400V 3-phase', N'ELC-VFD-3K', N'5901234567907', 6, 1, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 19)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (19, N'PROD-019', N'Welding Wire 1.0mm', N'MIG welding wire, ER70S-6, 15kg spool', N'RAW-WLD-10', N'5901234567908', 1, 2, 1, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 20)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (20, N'PROD-020', N'Control Panel CP-100', N'Assembled control panel with PLC and HMI', N'FIN-CP-100', N'5901234567909', 2, 1, 1, 1, 0, '2026-04-01T00:00:00', 1);

-- Inactive product
IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 21)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (21, N'PROD-021', N'Legacy Connector Type-B', N'Discontinued connector, Type-B interface', N'ELC-CON-TB', NULL, 6, 1, 0, 0, 0, '2026-04-01T00:00:00', 1);

-- Soft-deleted products
IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 22)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId])
    VALUES (22, N'PROD-022', N'Obsolete Relay R12', N'12V relay, no longer stocked', NULL, NULL, 4, 1, 0, 0, 1, '2026-04-05T00:00:00', '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 23)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId])
    VALUES (23, N'PROD-023', N'Test Sample X', N'Quality test sample, removed from catalog', NULL, NULL, 1, 1, 0, 0, 1, '2026-04-06T00:00:00', '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 24)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (24, N'PROD-024', N'Plastic Sheet PE 3mm', N'High-density polyethylene sheet, 3mm, 1220x2440mm', N'RAW-PLS-PE3', N'5901234567910', 1, 7, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 25)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (25, N'PROD-025', N'Adhesive Tape 50mm', N'Industrial adhesive tape, 50mm x 50m', N'PKG-TAP-50', N'5901234567911', 3, 8, 0, 1, 0, '2026-04-01T00:00:00', 1);

SET IDENTITY_INSERT [inventory].[Products] OFF;
");
        }

        private static void SeedWarehouses(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[Warehouses] ON;

IF NOT EXISTS (SELECT 1 FROM [inventory].[Warehouses] WHERE [Id] = 1)
    INSERT INTO [inventory].[Warehouses] ([Id], [Code], [Name], [Address], [Notes], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (1, N'WH-SOFIA', N'Sofia Central Warehouse', N'Warehouse District, Sofia 1528, Bulgaria', N'Main distribution center', 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Warehouses] WHERE [Id] = 2)
    INSERT INTO [inventory].[Warehouses] ([Id], [Code], [Name], [Address], [Notes], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (2, N'WH-PLOVDIV', N'Plovdiv Regional Warehouse', N'Industrial Zone North, Plovdiv 4003, Bulgaria', N'Regional hub for southern distribution', 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Warehouses] WHERE [Id] = 3)
    INSERT INTO [inventory].[Warehouses] ([Id], [Code], [Name], [Address], [Notes], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (3, N'WH-VARNA', N'Varna Port Warehouse', N'Port Industrial Area, Varna 9000, Bulgaria', N'Import/export staging warehouse', 1, 0, '2026-04-01T00:00:00', 1);

SET IDENTITY_INSERT [inventory].[Warehouses] OFF;
");
        }

        private static void SeedZones(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[Zones] ON;

-- Sofia warehouse zones
IF NOT EXISTS (SELECT 1 FROM [inventory].[Zones] WHERE [Id] = 1)
    INSERT INTO [inventory].[Zones] ([Id], [WarehouseId], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (1, 1, N'A', N'Zone A — Receiving', N'Inbound goods receiving area', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[Zones] WHERE [Id] = 2)
    INSERT INTO [inventory].[Zones] ([Id], [WarehouseId], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (2, 1, N'B', N'Zone B — Bulk Storage', N'High-rack bulk storage for palletised goods', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[Zones] WHERE [Id] = 3)
    INSERT INTO [inventory].[Zones] ([Id], [WarehouseId], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (3, 1, N'C', N'Zone C — Picking', N'Order picking area with shelving', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[Zones] WHERE [Id] = 4)
    INSERT INTO [inventory].[Zones] ([Id], [WarehouseId], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (4, 1, N'D', N'Zone D — Dispatch', N'Outbound staging and dispatch area', '2026-04-01T00:00:00');

-- Plovdiv warehouse zones
IF NOT EXISTS (SELECT 1 FROM [inventory].[Zones] WHERE [Id] = 5)
    INSERT INTO [inventory].[Zones] ([Id], [WarehouseId], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (5, 2, N'A', N'Zone A — General Storage', N'Mixed storage area', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[Zones] WHERE [Id] = 6)
    INSERT INTO [inventory].[Zones] ([Id], [WarehouseId], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (6, 2, N'B', N'Zone B — Hazmat', N'Hazardous materials storage with ventilation', '2026-04-01T00:00:00');

-- Varna warehouse zones
IF NOT EXISTS (SELECT 1 FROM [inventory].[Zones] WHERE [Id] = 7)
    INSERT INTO [inventory].[Zones] ([Id], [WarehouseId], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (7, 3, N'A', N'Zone A — Import Staging', N'Container unloading and staging', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [inventory].[Zones] WHERE [Id] = 8)
    INSERT INTO [inventory].[Zones] ([Id], [WarehouseId], [Code], [Name], [Description], [CreatedAtUtc])
    VALUES (8, 3, N'B', N'Zone B — Export Staging', N'Export container loading area', '2026-04-01T00:00:00');

SET IDENTITY_INSERT [inventory].[Zones] OFF;
");
        }

        private static void SeedStorageLocations(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[StorageLocations] ON;

-- Sofia Zone A (Receiving) — 4 locations
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 1)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (1, 1, 1, N'A-DOCK-01', N'Dock 1', N'Bulk', 50.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 2)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (2, 1, 1, N'A-DOCK-02', N'Dock 2', N'Bulk', 50.0000, '2026-04-01T00:00:00');

-- Sofia Zone B (Bulk Storage) — 6 locations
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 3)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (3, 1, 2, N'B-R01-S01', N'Row 1, Shelf 1', N'Shelf', 20.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 4)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (4, 1, 2, N'B-R01-S02', N'Row 1, Shelf 2', N'Shelf', 20.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 5)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (5, 1, 2, N'B-R02-S01', N'Row 2, Shelf 1', N'Shelf', 20.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 6)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (6, 1, 2, N'B-R02-S02', N'Row 2, Shelf 2', N'Shelf', 20.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 7)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (7, 1, 2, N'B-BULK-01', N'Bulk Area 1', N'Bulk', 100.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 8)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (8, 1, 2, N'B-BULK-02', N'Bulk Area 2', N'Bulk', 100.0000, '2026-04-01T00:00:00');

-- Sofia Zone C (Picking) — 4 locations
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 9)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (9, 1, 3, N'C-BIN-001', N'Pick Bin 1', N'Bin', 5.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 10)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (10, 1, 3, N'C-BIN-002', N'Pick Bin 2', N'Bin', 5.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 11)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (11, 1, 3, N'C-BIN-003', N'Pick Bin 3', N'Bin', 5.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 12)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (12, 1, 3, N'C-BIN-004', N'Pick Bin 4', N'Bin', 5.0000, '2026-04-01T00:00:00');

-- Sofia Zone D (Dispatch) — 2 locations
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 13)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (13, 1, 4, N'D-STAGE-01', N'Staging Bay 1', N'Bulk', 40.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 14)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (14, 1, 4, N'D-STAGE-02', N'Staging Bay 2', N'Bulk', 40.0000, '2026-04-01T00:00:00');

-- Plovdiv Zone A — 4 locations
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 15)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (15, 2, 5, N'A-R01-S01', N'Row 1, Shelf 1', N'Shelf', 15.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 16)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (16, 2, 5, N'A-R01-S02', N'Row 1, Shelf 2', N'Shelf', 15.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 17)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (17, 2, 5, N'A-BULK-01', N'Bulk Area 1', N'Bulk', 60.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 18)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (18, 2, 5, N'A-BULK-02', N'Bulk Area 2', N'Bulk', 60.0000, '2026-04-01T00:00:00');

-- Plovdiv Zone B (Hazmat) — 2 locations
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 19)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (19, 2, 6, N'B-HAZ-01', N'Hazmat Bay 1', N'Shelf', 10.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 20)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (20, 2, 6, N'B-HAZ-02', N'Hazmat Bay 2', N'Shelf', 10.0000, '2026-04-01T00:00:00');

-- Varna Zone A — 2 locations
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 21)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (21, 3, 7, N'A-IMP-01', N'Import Bay 1', N'Bulk', 80.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 22)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (22, 3, 7, N'A-IMP-02', N'Import Bay 2', N'Bulk', 80.0000, '2026-04-01T00:00:00');

-- Varna Zone B — 2 locations
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 23)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (23, 3, 8, N'B-EXP-01', N'Export Bay 1', N'Bulk', 80.0000, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[StorageLocations] WHERE [Id] = 24)
    INSERT INTO [inventory].[StorageLocations] ([Id], [WarehouseId], [ZoneId], [Code], [Name], [LocationType], [Capacity], [CreatedAtUtc])
    VALUES (24, 3, 8, N'B-EXP-02', N'Export Bay 2', N'Bulk', 80.0000, '2026-04-01T00:00:00');

SET IDENTITY_INSERT [inventory].[StorageLocations] OFF;
");
        }

        private static void SeedBatches(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[Batches] ON;

-- Batches for batch-tracked products (3=Solvent, 4=Epoxy, 5=PCB, 12=Hydraulic Oil, 15=Sensor, 19=Welding Wire, 20=Control Panel)
IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 1)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (1, 3, N'SOL-2026-001', '2026-01-15', '2027-01-15', N'First 2026 production run', 1, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 2)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (2, 3, N'SOL-2026-002', '2026-03-01', '2027-03-01', N'Second batch', 1, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 3)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (3, 4, N'EPX-2026-A01', '2026-02-10', '2027-02-10', N'Standard 2K mix batch', 1, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 4)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (4, 5, N'PCB-X1-2026-R3', '2026-03-20', NULL, N'Revision 3 board run', 1, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 5)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (5, 12, N'HYD46-2025-Q4', '2025-10-01', '2028-10-01', N'Long-shelf-life batch', 1, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 6)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (6, 15, N'SM200-2026-01', '2026-01-05', NULL, N'January production', 1, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 7)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (7, 19, N'WW10-2026-SP1', '2026-02-20', '2029-02-20', N'Spool lot 1', 1, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 8)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (8, 20, N'CP100-2026-S01', '2026-03-15', NULL, N'Serial run S01', 1, '2026-04-01T00:00:00', 1);

-- Expired batch
IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 9)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (9, 3, N'SOL-2024-OLD', '2024-01-01', '2025-01-01', N'Expired batch — held for disposal', 0, '2026-04-01T00:00:00', 1);

-- Inactive batch
IF NOT EXISTS (SELECT 1 FROM [inventory].[Batches] WHERE [Id] = 10)
    INSERT INTO [inventory].[Batches] ([Id], [ProductId], [BatchNumber], [ManufacturingDate], [ExpiryDate], [Notes], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (10, 4, N'EPX-2025-DISC', '2025-06-01', '2026-06-01', N'Discontinued formulation', 0, '2026-04-01T00:00:00', 1);

SET IDENTITY_INSERT [inventory].[Batches] OFF;
");
        }

        private static void SeedStockLevels(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[StockLevels] ON;

-- Sofia warehouse stock (various locations)
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 1)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (1, 1, 1, 3, NULL, 150.0000, 10.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 2)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (2, 2, 1, 4, NULL, 300.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 3)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (3, 3, 1, 5, 1, 80.0000, 5.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 4)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (4, 3, 1, 5, 2, 40.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 5)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (5, 4, 1, 6, 3, 25.0000, 2.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 6)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (6, 5, 1, 9, 4, 200.0000, 20.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 7)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (7, 6, 1, 10, NULL, 50.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 8)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (8, 7, 1, 7, NULL, 500.0000, 50.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 9)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (9, 8, 1, 8, NULL, 30.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 10)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (10, 9, 1, 7, NULL, 80.0000, 5.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 11)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (11, 10, 1, 11, NULL, 400.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 12)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (12, 11, 1, 11, NULL, 60.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 13)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (13, 13, 1, 3, NULL, 500.0000, 50.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 14)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (14, 14, 1, 12, NULL, 2000.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 15)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (15, 15, 1, 10, 6, 75.0000, 10.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 16)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (16, 16, 1, 12, NULL, 300.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 17)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (17, 17, 1, 8, NULL, 15.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 18)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (18, 18, 1, 9, NULL, 12.0000, 2.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 19)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (19, 19, 1, 6, 7, 45.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 20)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (20, 20, 1, 9, 8, 8.0000, 1.0000);

-- Plovdiv warehouse stock
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 21)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (21, 1, 2, 15, NULL, 75.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 22)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (22, 7, 2, 17, NULL, 200.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 23)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (23, 3, 2, 19, 1, 30.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 24)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (24, 12, 2, 20, 5, 60.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 25)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (25, 10, 2, 16, NULL, 150.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 26)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (26, 14, 2, 15, NULL, 800.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 27)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (27, 24, 2, 18, NULL, 40.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 28)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (28, 25, 2, 16, NULL, 100.0000, 0.0000);

-- Varna warehouse stock
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 29)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (29, 1, 3, 21, NULL, 200.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 30)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (30, 2, 3, 21, NULL, 500.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 31)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (31, 9, 3, 22, NULL, 120.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 32)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (32, 13, 3, 22, NULL, 250.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 33)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (33, 6, 3, 23, NULL, 30.0000, 5.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 34)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (34, 18, 3, 23, NULL, 8.0000, 0.0000);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockLevels] WHERE [Id] = 35)
    INSERT INTO [inventory].[StockLevels] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [QuantityOnHand], [QuantityReserved])
    VALUES (35, 20, 3, 24, 8, 5.0000, 0.0000);

SET IDENTITY_INSERT [inventory].[StockLevels] OFF;
");
        }

        private static void SeedStockMovements(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[StockMovements] ON;

-- Receipt movements — initial stock arrivals
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 1)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (1, 1, 1, 3, NULL, 150.0000, N'Receipt', NULL, N'PO-2026-001', N'Initial stock receipt', '2026-04-01T08:00:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 2)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (2, 2, 1, 4, NULL, 300.0000, N'Receipt', NULL, N'PO-2026-001', N'Initial stock receipt', '2026-04-01T08:15:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 3)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (3, 3, 1, 5, 1, 80.0000, N'Receipt', NULL, N'PO-2026-002', N'Solvent batch SOL-2026-001', '2026-04-01T09:00:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 4)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (4, 3, 1, 5, 2, 40.0000, N'Receipt', NULL, N'PO-2026-005', N'Solvent batch SOL-2026-002', '2026-04-03T10:00:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 5)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (5, 5, 1, 9, 4, 200.0000, N'Receipt', NULL, N'PO-2026-003', N'PCB Rev.3 boards received', '2026-04-01T10:30:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 6)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (6, 7, 1, 7, NULL, 500.0000, N'Receipt', NULL, N'PO-2026-004', N'Corrugated boxes bulk delivery', '2026-04-01T11:00:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 7)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (7, 10, 1, 11, NULL, 400.0000, N'Receipt', NULL, N'PO-2026-004', N'Bearings from SKF', '2026-04-01T11:30:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 8)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (8, 14, 1, 12, NULL, 2000.0000, N'Receipt', NULL, N'PO-2026-006', N'Stainless fasteners delivery', '2026-04-02T08:00:00', 1);

-- Shipment movements
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 9)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (9, 5, 1, 9, 4, -15.0000, N'Shipment', NULL, N'SO-2026-001', N'Shipped to Berlin Import GmbH', '2026-04-03T14:00:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 10)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (10, 7, 1, 7, NULL, -30.0000, N'Shipment', NULL, N'SO-2026-002', N'Shipped to Plovdiv Commerce EOOD', '2026-04-04T09:00:00', 1);

-- Transfer movements (Sofia → Plovdiv)
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 11)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (11, 1, 1, 3, NULL, -25.0000, N'Transfer', NULL, N'TRF-2026-001', N'Transfer out to Plovdiv', '2026-04-02T10:00:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 12)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (12, 1, 2, 15, NULL, 25.0000, N'Transfer', NULL, N'TRF-2026-001', N'Transfer in from Sofia', '2026-04-02T14:00:00', 1);

-- Adjustment movement
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 13)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (13, 14, 1, 12, NULL, -50.0000, N'Adjustment', NULL, N'ADJ-2026-001', N'Damaged in transit — write-down', '2026-04-05T11:00:00', 1);

-- Varna receipts
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 14)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (14, 1, 3, 21, NULL, 200.0000, N'Receipt', NULL, N'PO-2026-007', N'Import container from Turkey', '2026-04-03T07:00:00', 1);
IF NOT EXISTS (SELECT 1 FROM [inventory].[StockMovements] WHERE [Id] = 15)
    INSERT INTO [inventory].[StockMovements] ([Id], [ProductId], [WarehouseId], [LocationId], [BatchId], [Quantity], [ReasonCode], [ReferenceType], [ReferenceNumber], [Notes], [CreatedAtUtc], [CreatedByUserId])
    VALUES (15, 2, 3, 21, NULL, 500.0000, N'Receipt', NULL, N'PO-2026-007', N'Import container from Turkey', '2026-04-03T07:30:00', 1);

SET IDENTITY_INSERT [inventory].[StockMovements] OFF;
");
        }
    }
}
