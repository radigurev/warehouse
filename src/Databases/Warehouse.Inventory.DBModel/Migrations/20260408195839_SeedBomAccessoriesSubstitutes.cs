using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Inventory.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class SeedBomAccessoriesSubstitutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedAdditionalProducts(migrationBuilder);
            SeedBillOfMaterials(migrationBuilder);
            SeedBomLines(migrationBuilder);
            SeedProductAccessories(migrationBuilder);
            SeedProductSubstitutes(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [inventory].[ProductSubstitutes] WHERE [Id] BETWEEN 1 AND 8;
DELETE FROM [inventory].[ProductAccessories] WHERE [Id] BETWEEN 1 AND 10;
DELETE FROM [inventory].[BomLines] WHERE [Id] BETWEEN 1 AND 12;
DELETE FROM [inventory].[BillOfMaterials] WHERE [Id] BETWEEN 1 AND 3;
DELETE FROM [inventory].[Products] WHERE [Id] BETWEEN 26 AND 30;
");
        }

        private static void SeedAdditionalProducts(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[Products] ON;

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 26)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (26, N'PROD-026', N'Steel Sheet 3mm', N'Cold-rolled steel sheet, 3mm thickness, 1000x2000mm', N'STL-SHT-3MM', N'5901234567912', 1, 1, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 27)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (27, N'PROD-027', N'Aluminium Rod 12mm', N'Aluminium alloy rod, 12mm diameter, 3m length', N'ALU-ROD-12', N'5901234567913', 1, 4, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 28)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (28, N'PROD-028', N'Sensor Module SM-300', N'Industrial pressure sensor, 4-20mA output', N'ELC-SNS-300', N'5901234567914', 6, 1, 1, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 29)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (29, N'PROD-029', N'Corrugated Box 600x400', N'Single-wall corrugated box, 600x400x300mm', N'PKG-BOX-6040', N'5901234567915', 3, 5, 0, 1, 0, '2026-04-01T00:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[Products] WHERE [Id] = 30)
    INSERT INTO [inventory].[Products] ([Id], [Code], [Name], [Description], [Sku], [Barcode], [CategoryId], [UnitOfMeasureId], [RequiresBatchTracking], [IsActive], [IsDeleted], [CreatedAtUtc], [CreatedByUserId])
    VALUES (30, N'PROD-030', N'Hydraulic Oil ISO 68', N'Hydraulic fluid, ISO VG 68, 20L drum', N'CHM-HYD-68', N'5901234567916', 5, 3, 1, 1, 0, '2026-04-01T00:00:00', 1);

SET IDENTITY_INSERT [inventory].[Products] OFF;
");
        }

        private static void SeedBillOfMaterials(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[BillOfMaterials] ON;

-- Control Panel CP-100 (product 20) — full assembly
IF NOT EXISTS (SELECT 1 FROM [inventory].[BillOfMaterials] WHERE [Id] = 1)
    INSERT INTO [inventory].[BillOfMaterials] ([Id], [ParentProductId], [Name], [Version], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (1, 20, N'Control Panel CP-100 Assembly', N'1.0', 1, '2026-04-01T00:00:00', 1);

-- Motor Drive VFD-3K (product 18) — sub-assembly
IF NOT EXISTS (SELECT 1 FROM [inventory].[BillOfMaterials] WHERE [Id] = 2)
    INSERT INTO [inventory].[BillOfMaterials] ([Id], [ParentProductId], [Name], [Version], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (2, 18, N'Motor Drive VFD-3K Assembly', N'2.1', 1, '2026-04-01T00:00:00', 1);

-- Large Box Packaging Kit (product 29)
IF NOT EXISTS (SELECT 1 FROM [inventory].[BillOfMaterials] WHERE [Id] = 3)
    INSERT INTO [inventory].[BillOfMaterials] ([Id], [ParentProductId], [Name], [Version], [IsActive], [CreatedAtUtc], [CreatedByUserId])
    VALUES (3, 29, N'Large Box Packaging Kit', N'1.0', 1, '2026-04-01T00:00:00', 1);

SET IDENTITY_INSERT [inventory].[BillOfMaterials] OFF;
");
        }

        private static void SeedBomLines(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[BomLines] ON;

-- BOM 1: Control Panel CP-100
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 1)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (1, 1, 5, 1.0000, N'Main controller PCB');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 2)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (2, 1, 6, 1.0000, N'24V power supply');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 3)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (3, 1, 15, 2.0000, N'Temperature sensors (2x)');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 4)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (4, 1, 13, 5.0000, N'Internal wiring (5 metres)');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 5)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (5, 1, 14, 12.0000, N'Mounting bolts M8x30 (12x)');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 6)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (6, 1, 16, 4.0000, N'Cable entry gaskets (4x)');

-- BOM 2: Motor Drive VFD-3K
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 7)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (7, 2, 5, 1.0000, N'Controller PCB');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 8)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (8, 2, 6, 1.0000, N'Internal PSU');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 9)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (9, 2, 13, 3.0000, N'Internal wiring (3 metres)');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 10)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (10, 2, 28, 1.0000, N'Pressure sensor for motor monitoring');

-- BOM 3: Large Box Packaging Kit
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 11)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (11, 3, 17, 1.0000, N'Bubble wrap inner lining (1 roll)');
IF NOT EXISTS (SELECT 1 FROM [inventory].[BomLines] WHERE [Id] = 12)
    INSERT INTO [inventory].[BomLines] ([Id], [BillOfMaterialsId], [ChildProductId], [Quantity], [Notes])
    VALUES (12, 3, 25, 1.0000, N'Sealing tape (1 roll)');

SET IDENTITY_INSERT [inventory].[BomLines] OFF;
");
        }

        private static void SeedProductAccessories(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[ProductAccessories] ON;

-- Steel Sheet 2mm + fasteners/gaskets
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 1)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (1, 1, 14, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 2)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (2, 1, 16, '2026-04-01T00:00:00');

-- PCB + PSU + wiring
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 3)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (3, 5, 6, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 4)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (4, 5, 13, '2026-04-01T00:00:00');

-- Control Panel + additional sensors
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 5)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (5, 20, 15, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 6)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (6, 20, 28, '2026-04-01T00:00:00');

-- Box + bubble wrap + tape
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 7)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (7, 7, 17, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 8)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (8, 7, 25, '2026-04-01T00:00:00');

-- Pallet + stretch wrap
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 9)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (9, 9, 8, '2026-04-01T00:00:00');

-- Motor Drive + copper wire for installation
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductAccessories] WHERE [Id] = 10)
    INSERT INTO [inventory].[ProductAccessories] ([Id], [ProductId], [AccessoryProductId], [CreatedAtUtc])
    VALUES (10, 18, 13, '2026-04-01T00:00:00');

SET IDENTITY_INSERT [inventory].[ProductAccessories] OFF;
");
        }

        private static void SeedProductSubstitutes(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[ProductSubstitutes] ON;

-- Steel Sheet 2mm <-> 3mm (different gauge, same material)
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductSubstitutes] WHERE [Id] = 1)
    INSERT INTO [inventory].[ProductSubstitutes] ([Id], [ProductId], [SubstituteProductId], [CreatedAtUtc])
    VALUES (1, 1, 26, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductSubstitutes] WHERE [Id] = 2)
    INSERT INTO [inventory].[ProductSubstitutes] ([Id], [ProductId], [SubstituteProductId], [CreatedAtUtc])
    VALUES (2, 26, 1, '2026-04-01T00:00:00');

-- Aluminium Rod 10mm <-> 12mm
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductSubstitutes] WHERE [Id] = 3)
    INSERT INTO [inventory].[ProductSubstitutes] ([Id], [ProductId], [SubstituteProductId], [CreatedAtUtc])
    VALUES (3, 2, 27, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductSubstitutes] WHERE [Id] = 4)
    INSERT INTO [inventory].[ProductSubstitutes] ([Id], [ProductId], [SubstituteProductId], [CreatedAtUtc])
    VALUES (4, 27, 2, '2026-04-01T00:00:00');

-- Sensor SM-200 <-> SM-300 (temperature vs pressure, same form factor)
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductSubstitutes] WHERE [Id] = 5)
    INSERT INTO [inventory].[ProductSubstitutes] ([Id], [ProductId], [SubstituteProductId], [CreatedAtUtc])
    VALUES (5, 15, 28, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductSubstitutes] WHERE [Id] = 6)
    INSERT INTO [inventory].[ProductSubstitutes] ([Id], [ProductId], [SubstituteProductId], [CreatedAtUtc])
    VALUES (6, 28, 15, '2026-04-01T00:00:00');

-- Hydraulic Oil ISO 46 <-> ISO 68 (different viscosity grade)
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductSubstitutes] WHERE [Id] = 7)
    INSERT INTO [inventory].[ProductSubstitutes] ([Id], [ProductId], [SubstituteProductId], [CreatedAtUtc])
    VALUES (7, 12, 30, '2026-04-01T00:00:00');
IF NOT EXISTS (SELECT 1 FROM [inventory].[ProductSubstitutes] WHERE [Id] = 8)
    INSERT INTO [inventory].[ProductSubstitutes] ([Id], [ProductId], [SubstituteProductId], [CreatedAtUtc])
    VALUES (8, 30, 12, '2026-04-01T00:00:00');

SET IDENTITY_INSERT [inventory].[ProductSubstitutes] OFF;
");
        }
    }
}
