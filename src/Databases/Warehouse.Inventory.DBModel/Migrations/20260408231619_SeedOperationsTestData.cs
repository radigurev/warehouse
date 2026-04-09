using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Inventory.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class SeedOperationsTestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedStocktakeSessions(migrationBuilder);
            SeedStocktakeCounts(migrationBuilder);
            SeedInventoryAdjustments(migrationBuilder);
            SeedInventoryAdjustmentLines(migrationBuilder);
            SeedWarehouseTransfers(migrationBuilder);
            SeedWarehouseTransferLines(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [inventory].[WarehouseTransferLines] WHERE [Id] BETWEEN 1 AND 6;
DELETE FROM [inventory].[WarehouseTransfers] WHERE [Id] BETWEEN 1 AND 4;
DELETE FROM [inventory].[InventoryAdjustmentLines] WHERE [Id] BETWEEN 1 AND 6;
DELETE FROM [inventory].[InventoryAdjustments] WHERE [Id] BETWEEN 1 AND 5;
DELETE FROM [inventory].[StocktakeCounts] WHERE [Id] BETWEEN 1 AND 8;
DELETE FROM [inventory].[StocktakeSessions] WHERE [Id] BETWEEN 1 AND 4;
");
        }

        private static void SeedInventoryAdjustments(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[InventoryAdjustments] ON;

-- Pending adjustment: cycle count discrepancy at Sofia warehouse
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustments] WHERE [Id] = 1)
    INSERT INTO [inventory].[InventoryAdjustments]
        ([Id], [Reason], [Notes], [Status], [CreatedAtUtc], [CreatedByUserId],
         [ApprovedAtUtc], [ApprovedByUserId], [RejectedAtUtc], [RejectedByUserId], [RejectionReason],
         [AppliedAtUtc], [AppliedByUserId], [SourceStocktakeSessionId])
    VALUES
        (1, N'Cycle count discrepancy', N'Monthly cycle count — Zone B bulk storage', N'Pending',
         '2026-04-05T08:30:00', 1,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

-- Approved adjustment: damaged goods write-off
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustments] WHERE [Id] = 2)
    INSERT INTO [inventory].[InventoryAdjustments]
        ([Id], [Reason], [Notes], [Status], [CreatedAtUtc], [CreatedByUserId],
         [ApprovedAtUtc], [ApprovedByUserId], [RejectedAtUtc], [RejectedByUserId], [RejectionReason],
         [AppliedAtUtc], [AppliedByUserId], [SourceStocktakeSessionId])
    VALUES
        (2, N'Damaged goods write-off', N'Water damage in Zone C — 3 bins affected', N'Approved',
         '2026-04-03T14:15:00', 1,
         '2026-04-04T09:00:00', 1, NULL, NULL, NULL, NULL, NULL, NULL);

-- Applied adjustment: receiving overcount correction
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustments] WHERE [Id] = 3)
    INSERT INTO [inventory].[InventoryAdjustments]
        ([Id], [Reason], [Notes], [Status], [CreatedAtUtc], [CreatedByUserId],
         [ApprovedAtUtc], [ApprovedByUserId], [RejectedAtUtc], [RejectedByUserId], [RejectionReason],
         [AppliedAtUtc], [AppliedByUserId], [SourceStocktakeSessionId])
    VALUES
        (3, N'Receiving overcount correction', N'Supplier shipment had 10 extra units misrecorded on receipt', N'Applied',
         '2026-04-01T10:00:00', 1,
         '2026-04-01T11:30:00', 1, NULL, NULL, NULL,
         '2026-04-01T13:00:00', 1, NULL);

-- Rejected adjustment: unverified shortage claim
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustments] WHERE [Id] = 4)
    INSERT INTO [inventory].[InventoryAdjustments]
        ([Id], [Reason], [Notes], [Status], [CreatedAtUtc], [CreatedByUserId],
         [ApprovedAtUtc], [ApprovedByUserId], [RejectedAtUtc], [RejectedByUserId], [RejectionReason],
         [AppliedAtUtc], [AppliedByUserId], [SourceStocktakeSessionId])
    VALUES
        (4, N'Shortage claim — Zone A dock', N'Reported 20 units missing from dock area', N'Rejected',
         '2026-04-02T16:00:00', 1,
         NULL, NULL, '2026-04-03T10:00:00', 1, N'CCTV review shows units were moved to Zone B, not missing',
         NULL, NULL, NULL);

-- Pending adjustment sourced from completed stocktake session (ID 3)
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustments] WHERE [Id] = 5)
    INSERT INTO [inventory].[InventoryAdjustments]
        ([Id], [Reason], [Notes], [Status], [CreatedAtUtc], [CreatedByUserId],
         [ApprovedAtUtc], [ApprovedByUserId], [RejectedAtUtc], [RejectedByUserId], [RejectionReason],
         [AppliedAtUtc], [AppliedByUserId], [SourceStocktakeSessionId])
    VALUES
        (5, N'Stocktake variance', N'Auto-generated from stocktake session #3: Q1 Plovdiv Full Count', N'Pending',
         '2026-04-07T15:00:00', 1,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 3);

SET IDENTITY_INSERT [inventory].[InventoryAdjustments] OFF;
");
        }

        private static void SeedInventoryAdjustmentLines(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[InventoryAdjustmentLines] ON;

-- Adjustment 1 lines (Pending — Sofia WH1, Zone B locations)
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustmentLines] WHERE [Id] = 1)
    INSERT INTO [inventory].[InventoryAdjustmentLines]
        ([Id], [AdjustmentId], [ProductId], [WarehouseId], [LocationId], [BatchId], [ExpectedQuantity], [ActualQuantity])
    VALUES (1, 1, 1, 1, 3, NULL, 150.0000, 147.0000);

IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustmentLines] WHERE [Id] = 2)
    INSERT INTO [inventory].[InventoryAdjustmentLines]
        ([Id], [AdjustmentId], [ProductId], [WarehouseId], [LocationId], [BatchId], [ExpectedQuantity], [ActualQuantity])
    VALUES (2, 1, 2, 1, 4, NULL, 300.0000, 298.0000);

-- Adjustment 2 lines (Approved — Sofia WH1, Zone C bin)
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustmentLines] WHERE [Id] = 3)
    INSERT INTO [inventory].[InventoryAdjustmentLines]
        ([Id], [AdjustmentId], [ProductId], [WarehouseId], [LocationId], [BatchId], [ExpectedQuantity], [ActualQuantity])
    VALUES (3, 2, 6, 1, 9, NULL, 25.0000, 20.0000);

-- Adjustment 3 lines (Applied — Sofia WH1)
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustmentLines] WHERE [Id] = 4)
    INSERT INTO [inventory].[InventoryAdjustmentLines]
        ([Id], [AdjustmentId], [ProductId], [WarehouseId], [LocationId], [BatchId], [ExpectedQuantity], [ActualQuantity])
    VALUES (4, 3, 7, 1, 5, NULL, 60.0000, 50.0000);

-- Adjustment 4 lines (Rejected — Sofia WH1, Zone A)
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustmentLines] WHERE [Id] = 5)
    INSERT INTO [inventory].[InventoryAdjustmentLines]
        ([Id], [AdjustmentId], [ProductId], [WarehouseId], [LocationId], [BatchId], [ExpectedQuantity], [ActualQuantity])
    VALUES (5, 4, 8, 1, 1, NULL, 40.0000, 20.0000);

-- Adjustment 5 lines (Pending from stocktake — Plovdiv WH2)
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryAdjustmentLines] WHERE [Id] = 6)
    INSERT INTO [inventory].[InventoryAdjustmentLines]
        ([Id], [AdjustmentId], [ProductId], [WarehouseId], [LocationId], [BatchId], [ExpectedQuantity], [ActualQuantity])
    VALUES (6, 5, 10, 2, 15, NULL, 45.0000, 42.0000);

SET IDENTITY_INSERT [inventory].[InventoryAdjustmentLines] OFF;
");
        }

        private static void SeedWarehouseTransfers(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[WarehouseTransfers] ON;

-- Draft transfer: Sofia -> Plovdiv
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransfers] WHERE [Id] = 1)
    INSERT INTO [inventory].[WarehouseTransfers]
        ([Id], [SourceWarehouseId], [DestinationWarehouseId], [Status], [Notes],
         [CreatedAtUtc], [CreatedByUserId], [CompletedAtUtc], [CompletedByUserId])
    VALUES
        (1, 1, 2, N'Draft', N'Restock Plovdiv from Sofia bulk storage',
         '2026-04-06T09:00:00', 1, NULL, NULL);

-- Completed transfer: Sofia -> Varna
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransfers] WHERE [Id] = 2)
    INSERT INTO [inventory].[WarehouseTransfers]
        ([Id], [SourceWarehouseId], [DestinationWarehouseId], [Status], [Notes],
         [CreatedAtUtc], [CreatedByUserId], [CompletedAtUtc], [CompletedByUserId])
    VALUES
        (2, 1, 3, N'Completed', N'Export staging replenishment',
         '2026-04-02T11:00:00', 1, '2026-04-03T16:00:00', 1);

-- Cancelled transfer: Plovdiv -> Sofia
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransfers] WHERE [Id] = 3)
    INSERT INTO [inventory].[WarehouseTransfers]
        ([Id], [SourceWarehouseId], [DestinationWarehouseId], [Status], [Notes],
         [CreatedAtUtc], [CreatedByUserId], [CompletedAtUtc], [CompletedByUserId])
    VALUES
        (3, 2, 1, N'Cancelled', N'Cancelled — stock already moved via direct shipment',
         '2026-04-04T07:30:00', 1, NULL, NULL);

-- Draft transfer: Varna -> Sofia
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransfers] WHERE [Id] = 4)
    INSERT INTO [inventory].[WarehouseTransfers]
        ([Id], [SourceWarehouseId], [DestinationWarehouseId], [Status], [Notes],
         [CreatedAtUtc], [CreatedByUserId], [CompletedAtUtc], [CompletedByUserId])
    VALUES
        (4, 3, 1, N'Draft', N'Return surplus import stock to Sofia',
         '2026-04-07T13:00:00', 1, NULL, NULL);

SET IDENTITY_INSERT [inventory].[WarehouseTransfers] OFF;
");
        }

        private static void SeedWarehouseTransferLines(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[WarehouseTransferLines] ON;

-- Transfer 1 lines (Draft: Sofia -> Plovdiv)
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransferLines] WHERE [Id] = 1)
    INSERT INTO [inventory].[WarehouseTransferLines]
        ([Id], [TransferId], [ProductId], [Quantity], [SourceLocationId], [DestinationLocationId])
    VALUES (1, 1, 1, 30.0000, 3, 15);

IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransferLines] WHERE [Id] = 2)
    INSERT INTO [inventory].[WarehouseTransferLines]
        ([Id], [TransferId], [ProductId], [Quantity], [SourceLocationId], [DestinationLocationId])
    VALUES (2, 1, 2, 50.0000, 4, 16);

-- Transfer 2 lines (Completed: Sofia -> Varna)
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransferLines] WHERE [Id] = 3)
    INSERT INTO [inventory].[WarehouseTransferLines]
        ([Id], [TransferId], [ProductId], [Quantity], [SourceLocationId], [DestinationLocationId])
    VALUES (3, 2, 8, 15.0000, 7, 21);

-- Transfer 3 lines (Cancelled: Plovdiv -> Sofia)
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransferLines] WHERE [Id] = 4)
    INSERT INTO [inventory].[WarehouseTransferLines]
        ([Id], [TransferId], [ProductId], [Quantity], [SourceLocationId], [DestinationLocationId])
    VALUES (4, 3, 10, 20.0000, 15, 3);

-- Transfer 4 lines (Draft: Varna -> Sofia)
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransferLines] WHERE [Id] = 5)
    INSERT INTO [inventory].[WarehouseTransferLines]
        ([Id], [TransferId], [ProductId], [Quantity], [SourceLocationId], [DestinationLocationId])
    VALUES (5, 4, 14, 10.0000, 21, 7);

IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseTransferLines] WHERE [Id] = 6)
    INSERT INTO [inventory].[WarehouseTransferLines]
        ([Id], [TransferId], [ProductId], [Quantity], [SourceLocationId], [DestinationLocationId])
    VALUES (6, 4, 16, 25.0000, 23, 8);

SET IDENTITY_INSERT [inventory].[WarehouseTransferLines] OFF;
");
        }

        private static void SeedStocktakeSessions(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[StocktakeSessions] ON;

-- Draft session: Sofia Zone A
IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeSessions] WHERE [Id] = 1)
    INSERT INTO [inventory].[StocktakeSessions]
        ([Id], [WarehouseId], [ZoneId], [Name], [Notes], [Status],
         [CreatedAtUtc], [CreatedByUserId], [StartedAtUtc], [CompletedAtUtc], [CompletedByUserId])
    VALUES
        (1, 1, 1, N'April Sofia Receiving Dock Count', N'Scheduled weekly dock area count', N'Draft',
         '2026-04-08T07:00:00', 1, NULL, NULL, NULL);

-- InProgress session: Sofia Zone C (Picking)
IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeSessions] WHERE [Id] = 2)
    INSERT INTO [inventory].[StocktakeSessions]
        ([Id], [WarehouseId], [ZoneId], [Name], [Notes], [Status],
         [CreatedAtUtc], [CreatedByUserId], [StartedAtUtc], [CompletedAtUtc], [CompletedByUserId])
    VALUES
        (2, 1, 3, N'April Pick Zone Audit', N'Quarterly picking zone accuracy audit', N'InProgress',
         '2026-04-06T06:00:00', 1, '2026-04-06T08:00:00', NULL, NULL);

-- Completed session: Plovdiv Zone A
IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeSessions] WHERE [Id] = 3)
    INSERT INTO [inventory].[StocktakeSessions]
        ([Id], [WarehouseId], [ZoneId], [Name], [Notes], [Status],
         [CreatedAtUtc], [CreatedByUserId], [StartedAtUtc], [CompletedAtUtc], [CompletedByUserId])
    VALUES
        (3, 2, 5, N'Q1 Plovdiv Full Count', N'End of Q1 mandatory full inventory count', N'Completed',
         '2026-04-01T06:00:00', 1, '2026-04-01T08:00:00', '2026-04-01T17:00:00', 1);

-- Cancelled session: Varna
IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeSessions] WHERE [Id] = 4)
    INSERT INTO [inventory].[StocktakeSessions]
        ([Id], [WarehouseId], [ZoneId], [Name], [Notes], [Status],
         [CreatedAtUtc], [CreatedByUserId], [StartedAtUtc], [CompletedAtUtc], [CompletedByUserId])
    VALUES
        (4, 3, 7, N'Varna Import Bay Count', N'Cancelled due to incoming shipment blocking access', N'Cancelled',
         '2026-04-05T09:00:00', 1, NULL, NULL, NULL);

SET IDENTITY_INSERT [inventory].[StocktakeSessions] OFF;
");
        }

        private static void SeedStocktakeCounts(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [inventory].[StocktakeCounts] ON;

-- Session 2 counts (InProgress — Sofia Zone C picking bins, products 6, 9, 11)
IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeCounts] WHERE [Id] = 1)
    INSERT INTO [inventory].[StocktakeCounts]
        ([Id], [SessionId], [ProductId], [LocationId], [ExpectedQuantity], [ActualQuantity], [Variance], [CountedAtUtc], [CountedByUserId])
    VALUES (1, 2, 6, 9, 25.0000, 24.0000, -1.0000, '2026-04-06T09:15:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeCounts] WHERE [Id] = 2)
    INSERT INTO [inventory].[StocktakeCounts]
        ([Id], [SessionId], [ProductId], [LocationId], [ExpectedQuantity], [ActualQuantity], [Variance], [CountedAtUtc], [CountedByUserId])
    VALUES (2, 2, 9, 10, 35.0000, 35.0000, 0.0000, '2026-04-06T09:30:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeCounts] WHERE [Id] = 3)
    INSERT INTO [inventory].[StocktakeCounts]
        ([Id], [SessionId], [ProductId], [LocationId], [ExpectedQuantity], [ActualQuantity], [Variance], [CountedAtUtc], [CountedByUserId])
    VALUES (3, 2, 11, 11, 18.0000, 20.0000, 2.0000, '2026-04-06T09:45:00', 1);

-- Session 3 counts (Completed — Plovdiv Zone A, products 10, 13, 14, 16, 17)
IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeCounts] WHERE [Id] = 4)
    INSERT INTO [inventory].[StocktakeCounts]
        ([Id], [SessionId], [ProductId], [LocationId], [ExpectedQuantity], [ActualQuantity], [Variance], [CountedAtUtc], [CountedByUserId])
    VALUES (4, 3, 10, 15, 45.0000, 42.0000, -3.0000, '2026-04-01T10:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeCounts] WHERE [Id] = 5)
    INSERT INTO [inventory].[StocktakeCounts]
        ([Id], [SessionId], [ProductId], [LocationId], [ExpectedQuantity], [ActualQuantity], [Variance], [CountedAtUtc], [CountedByUserId])
    VALUES (5, 3, 13, 16, 30.0000, 30.0000, 0.0000, '2026-04-01T10:30:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeCounts] WHERE [Id] = 6)
    INSERT INTO [inventory].[StocktakeCounts]
        ([Id], [SessionId], [ProductId], [LocationId], [ExpectedQuantity], [ActualQuantity], [Variance], [CountedAtUtc], [CountedByUserId])
    VALUES (6, 3, 14, 17, 60.0000, 58.0000, -2.0000, '2026-04-01T11:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeCounts] WHERE [Id] = 7)
    INSERT INTO [inventory].[StocktakeCounts]
        ([Id], [SessionId], [ProductId], [LocationId], [ExpectedQuantity], [ActualQuantity], [Variance], [CountedAtUtc], [CountedByUserId])
    VALUES (7, 3, 16, 17, 40.0000, 40.0000, 0.0000, '2026-04-01T11:30:00', 1);

IF NOT EXISTS (SELECT 1 FROM [inventory].[StocktakeCounts] WHERE [Id] = 8)
    INSERT INTO [inventory].[StocktakeCounts]
        ([Id], [SessionId], [ProductId], [LocationId], [ExpectedQuantity], [ActualQuantity], [Variance], [CountedAtUtc], [CountedByUserId])
    VALUES (8, 3, 17, 18, 25.0000, 23.0000, -2.0000, '2026-04-01T12:00:00', 1);

SET IDENTITY_INSERT [inventory].[StocktakeCounts] OFF;
");
        }
    }
}
