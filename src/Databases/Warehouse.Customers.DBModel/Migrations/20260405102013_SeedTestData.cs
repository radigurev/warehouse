using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Customers.DBModel.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedCustomerCategories(migrationBuilder);
            SeedCustomers(migrationBuilder);
            SeedCustomerAccounts(migrationBuilder);
            SeedCustomerAddresses(migrationBuilder);
            SeedCustomerPhones(migrationBuilder);
            SeedCustomerEmails(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [customers].[CustomerEmails] WHERE [Id] BETWEEN 1 AND 30;
DELETE FROM [customers].[CustomerPhones] WHERE [Id] BETWEEN 1 AND 30;
DELETE FROM [customers].[CustomerAddresses] WHERE [Id] BETWEEN 1 AND 40;
DELETE FROM [customers].[CustomerAccounts] WHERE [Id] BETWEEN 1 AND 50;
DELETE FROM [customers].[Customers] WHERE [Id] BETWEEN 1 AND 30;
DELETE FROM [customers].[CustomerCategories] WHERE [Id] BETWEEN 1 AND 5;
");
        }

        private static void SeedCustomerCategories(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [customers].[CustomerCategories] ON;

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerCategories] WHERE [Id] = 1)
    INSERT INTO [customers].[CustomerCategories] ([Id], [Name], [Description], [CreatedAtUtc])
    VALUES (1, N'Wholesale', N'Wholesale buyers purchasing in bulk quantities', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerCategories] WHERE [Id] = 2)
    INSERT INTO [customers].[CustomerCategories] ([Id], [Name], [Description], [CreatedAtUtc])
    VALUES (2, N'Retail', N'Retail customers purchasing individual items', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerCategories] WHERE [Id] = 3)
    INSERT INTO [customers].[CustomerCategories] ([Id], [Name], [Description], [CreatedAtUtc])
    VALUES (3, N'Distributor', N'Authorized distributors managing regional supply chains', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerCategories] WHERE [Id] = 4)
    INSERT INTO [customers].[CustomerCategories] ([Id], [Name], [Description], [CreatedAtUtc])
    VALUES (4, N'Manufacturer', N'Manufacturing partners requiring raw materials and components', '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerCategories] WHERE [Id] = 5)
    INSERT INTO [customers].[CustomerCategories] ([Id], [Name], [Description], [CreatedAtUtc])
    VALUES (5, N'Service Provider', N'Service companies providing logistics and support', '2026-04-01T00:00:00');

SET IDENTITY_INSERT [customers].[CustomerCategories] OFF;
");
        }

        private static void SeedCustomers(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [customers].[Customers] ON;

-- Wholesale customers (CategoryId = 1)
IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 1)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (1, N'CUST-000001', N'Sofia Trading OOD', N'BG175342891', 1, N'Major wholesale partner for Sofia region', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 2)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (2, N'CUST-000002', N'Plovdiv Logistics EOOD', N'BG204568173', 1, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 3)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (3, N'CUST-000003', N'Berlin Import GmbH', N'DE287654321', 1, N'German wholesale partner for Central European distribution', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 4)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (4, N'CUST-000004', N'Bucharest Electronics SRL', N'RO19284756', 1, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 5)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (5, N'CUST-000005', N'Varna Wholesale Group AD', N'BG831297465', 1, N'Black Sea region wholesale operations', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 6)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (6, N'CUST-000006', N'Budapest Supply Kft.', N'HU12345678', 1, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

-- Retail customers (CategoryId = 2)
IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 7)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (7, N'CUST-000007', N'Burgas Retail Store EOOD', N'BG946152738', 2, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 8)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (8, N'CUST-000008', N'Ruse Market OOD', N'BG517293846', 2, N'Regional retail chain with 5 locations in Ruse', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 9)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (9, N'CUST-000009', N'Stara Zagora Supermarket AD', N'BG362918475', 2, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 10)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (10, N'CUST-000010', N'Wien Handels AG', N'ATU63472819', 2, N'Austrian retail partner', 0, 0, NULL, '2026-04-01T00:00:00', 1, '2026-04-03T10:30:00', 1);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 11)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (11, N'CUST-000011', N'Thessaloniki Shop SA', N'EL094837261', 2, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 12)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (12, N'CUST-000012', N'Blagoevgrad Commerce EOOD', N'BG728194635', 2, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

-- Distributor customers (CategoryId = 3)
IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 13)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (13, N'CUST-000013', N'Balkan Distribution OOD', N'BG291847563', 3, N'Primary distributor for Balkans region', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 14)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (14, N'CUST-000014', N'Praha Distribution s.r.o.', N'CZ65432198', 3, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 15)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (15, N'CUST-000015', N'Warszawa Logistics Sp. z o.o.', N'PL5472918364', 3, N'Polish distribution hub covering Eastern Europe', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 16)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (16, N'CUST-000016', N'Gabrovo Express EOOD', N'BG648291375', 3, NULL, 0, 0, NULL, '2026-04-01T00:00:00', 1, '2026-04-02T14:00:00', 1);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 17)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (17, N'CUST-000017', N'Bratislava Partners s.r.o.', N'SK2023456789', 3, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 18)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (18, N'CUST-000018', N'Veliko Tarnovo Supply AD', N'BG483726195', 3, N'Historical trade center for North Bulgaria distribution', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

-- Manufacturer customers (CategoryId = 4)
IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 19)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (19, N'CUST-000019', N'Pernik Manufacturing OOD', N'BG195847362', 4, N'Steel and metal parts manufacturer', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 20)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (20, N'CUST-000020', N'Milano Produzione S.r.l.', N'IT04827163950', 4, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 21)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (21, N'CUST-000021', N'Pleven Industrial EOOD', N'BG572938164', 4, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 22)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (22, N'CUST-000022', N'Hamburg Werke GmbH', N'DE198273645', 4, N'German heavy machinery manufacturer', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 23)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (23, N'CUST-000023', N'Kazanlak Rose Products AD', N'BG837461925', 4, N'Rose oil and cosmetics manufacturer from the Valley of Roses', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 24)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (24, N'CUST-000024', N'Cluj Electronics SRL', N'RO28374651', 4, NULL, 1, 1, '2026-04-03T08:00:00', '2026-04-01T00:00:00', 1, '2026-04-03T08:00:00', 1);

-- Service Provider customers (CategoryId = 5)
IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 25)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (25, N'CUST-000025', N'Sofia Freight Services EOOD', N'BG462918374', 5, N'Freight and customs brokerage services', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 26)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (26, N'CUST-000026', N'Balkan IT Solutions OOD', N'BG918273645', 5, NULL, 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 27)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (27, N'CUST-000027', N'Dobrich Transport AD', N'BG374618295', 5, NULL, 0, 0, NULL, '2026-04-01T00:00:00', 1, '2026-04-02T16:45:00', 1);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 28)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (28, N'CUST-000028', N'Athens Consulting SA', N'EL837291645', 5, N'Greek consulting firm for Southeast European operations', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 29)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (29, N'CUST-000029', N'Shumen Packaging EOOD', N'BG261837495', 5, NULL, 1, 1, '2026-04-04T11:20:00', '2026-04-01T00:00:00', 1, '2026-04-04T11:20:00', 1);

IF NOT EXISTS (SELECT 1 FROM [customers].[Customers] WHERE [Id] = 30)
    INSERT INTO [customers].[Customers] ([Id], [Code], [Name], [TaxId], [CategoryId], [Notes], [IsActive], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc], [CreatedByUserId], [ModifiedAtUtc], [ModifiedByUserId])
    VALUES (30, N'CUST-000030', N'Haskovo Maintenance OOD', N'BG729384615', 5, N'Facility maintenance and cleaning services', 1, 0, NULL, '2026-04-01T00:00:00', 1, NULL, NULL);

SET IDENTITY_INSERT [customers].[Customers] OFF;
");
        }

        private static void SeedCustomerAccounts(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [customers].[CustomerAccounts] ON;

-- Customer 1: Sofia Trading OOD (3 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 1)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (1, 1, N'BGN', 125430.5000, N'Primary BGN operating account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 2)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (2, 1, N'EUR', 48750.2500, N'EUR account for EU transactions', 0, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 3)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (3, 1, N'USD', 0.0000, N'USD account for overseas payments', 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 2: Plovdiv Logistics EOOD (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 4)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (4, 2, N'BGN', 87210.7500, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 5)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (5, 2, N'EUR', 22100.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 3: Berlin Import GmbH (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 6)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (6, 3, N'EUR', 312500.0000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 7)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (7, 3, N'USD', 15000.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 4: Bucharest Electronics SRL (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 8)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (8, 4, N'EUR', 95200.5000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 9)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (9, 4, N'USD', 7800.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 5: Varna Wholesale Group AD (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 10)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (10, 5, N'BGN', 254870.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 11)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (11, 5, N'EUR', 63200.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 6: Budapest Supply Kft. (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 12)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (12, 6, N'EUR', 41500.2500, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 7: Burgas Retail Store EOOD (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 13)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (13, 7, N'BGN', 18300.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 14)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (14, 7, N'EUR', 3500.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 8: Ruse Market OOD (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 15)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (15, 8, N'BGN', 29750.5000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 9: Stara Zagora Supermarket AD (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 16)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (16, 9, N'BGN', 45600.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 10: Wien Handels AG (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 17)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (17, 10, N'EUR', 0.0000, N'Primary EUR account - inactive customer', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 18)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (18, 10, N'GBP', 1200.0000, NULL, 0, 1, '2026-04-03T10:30:00', '2026-04-01T00:00:00');

-- Customer 11: Thessaloniki Shop SA (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 19)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (19, 11, N'EUR', 16890.0000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 12: Blagoevgrad Commerce EOOD (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 20)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (20, 12, N'BGN', 8920.7500, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 13: Balkan Distribution OOD (3 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 21)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (21, 13, N'BGN', 187600.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 22)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (22, 13, N'EUR', 92350.0000, N'EUR account for cross-border distribution', 0, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 23)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (23, 13, N'USD', 28400.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 14: Praha Distribution s.r.o. (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 24)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (24, 14, N'EUR', 54700.0000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 15: Warszawa Logistics Sp. z o.o. (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 25)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (25, 15, N'EUR', 118300.0000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 26)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (26, 15, N'USD', 34000.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 16: Gabrovo Express EOOD (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 27)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (27, 16, N'BGN', 0.0000, N'Primary BGN account - inactive customer', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 17: Bratislava Partners s.r.o. (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 28)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (28, 17, N'EUR', 27350.0000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 18: Veliko Tarnovo Supply AD (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 29)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (29, 18, N'BGN', 76430.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 30)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (30, 18, N'EUR', 19200.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 19: Pernik Manufacturing OOD (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 31)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (31, 19, N'BGN', 498200.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 32)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (32, 19, N'EUR', 145600.0000, N'EUR account for equipment imports', 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 20: Milano Produzione S.r.l. (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 33)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (33, 20, N'EUR', 267400.0000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 21: Pleven Industrial EOOD (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 34)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (34, 21, N'BGN', 53100.2500, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 22: Hamburg Werke GmbH (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 35)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (35, 22, N'EUR', 385000.0000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 36)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (36, 22, N'GBP', 42000.0000, N'GBP account for UK operations', 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 23: Kazanlak Rose Products AD (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 37)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (37, 23, N'BGN', 142800.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 24: Cluj Electronics SRL (1 account - soft deleted with customer)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 38)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (38, 24, N'EUR', 11200.0000, N'Primary EUR account', 1, 1, '2026-04-03T08:00:00', '2026-04-01T00:00:00');

-- Customer 25: Sofia Freight Services EOOD (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 39)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (39, 25, N'BGN', 67340.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 40)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (40, 25, N'EUR', 23100.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 26: Balkan IT Solutions OOD (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 41)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (41, 26, N'BGN', 34500.0000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 27: Dobrich Transport AD (1 account)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 42)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (42, 27, N'BGN', 0.0000, N'Primary BGN account - inactive customer', 1, 0, NULL, '2026-04-01T00:00:00');

-- Customer 28: Athens Consulting SA (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 43)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (43, 28, N'EUR', 89400.0000, N'Primary EUR account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 44)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (44, 28, N'USD', 12750.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 29: Shumen Packaging EOOD (1 account - soft deleted with customer)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 45)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (45, 29, N'BGN', 5400.0000, N'Primary BGN account', 1, 1, '2026-04-04T11:20:00', '2026-04-01T00:00:00');

-- Customer 30: Haskovo Maintenance OOD (2 accounts)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 46)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (46, 30, N'BGN', 21700.5000, N'Primary BGN account', 1, 0, NULL, '2026-04-01T00:00:00');

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 47)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (47, 30, N'EUR', 4800.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Additional accounts to reach 50 total
-- Customer 3: Berlin Import GmbH (add GBP)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 48)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (48, 3, N'GBP', 28500.0000, N'GBP account for UK trade', 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 15: Warszawa Logistics (add GBP)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 49)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (49, 15, N'GBP', 9600.0000, NULL, 0, 0, NULL, '2026-04-01T00:00:00');

-- Customer 20: Milano Produzione (add USD)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAccounts] WHERE [Id] = 50)
    INSERT INTO [customers].[CustomerAccounts] ([Id], [CustomerId], [CurrencyCode], [Balance], [Description], [IsPrimary], [IsDeleted], [DeletedAtUtc], [CreatedAtUtc])
    VALUES (50, 20, N'USD', 74200.0000, N'USD account for North American exports', 0, 0, NULL, '2026-04-01T00:00:00');

SET IDENTITY_INSERT [customers].[CustomerAccounts] OFF;
");
        }

        private static void SeedCustomerAddresses(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [customers].[CustomerAddresses] ON;

-- Customer 1: Sofia Trading OOD (2 addresses)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 1)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (1, 1, N'Billing', N'bul. Vitosha 89', N'fl. 3', N'Sofia', N'Sofia-grad', N'1000', N'BG', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 2)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (2, 1, N'Shipping', N'ul. Okolovrastno shose 215', NULL, N'Sofia', N'Sofia-grad', N'1138', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 2: Plovdiv Logistics EOOD (2 addresses)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 3)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (3, 2, N'Both', N'bul. Maritsa 122', NULL, N'Plovdiv', N'Plovdiv', N'4000', N'BG', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 4)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (4, 2, N'Shipping', N'Industrialna zona Trakia', N'Hala 7', N'Plovdiv', N'Plovdiv', N'4023', N'BG', 0, '2026-04-01T00:00:00', NULL);

-- Customer 3: Berlin Import GmbH (2 addresses)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 5)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (5, 3, N'Billing', N'Friedrichstrasse 148', NULL, N'Berlin', N'Berlin', N'10117', N'DE', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 6)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (6, 3, N'Shipping', N'Industriestrasse 42', N'Tor 3', N'Berlin', N'Berlin', N'12099', N'DE', 1, '2026-04-01T00:00:00', NULL);

-- Customer 4: Bucharest Electronics SRL (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 7)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (7, 4, N'Both', N'Calea Victoriei 155', N'Et. 4', N'Bucharest', N'Bucuresti', N'010072', N'RO', 1, '2026-04-01T00:00:00', NULL);

-- Customer 5: Varna Wholesale Group AD (2 addresses)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 8)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (8, 5, N'Billing', N'bul. Slivnitsa 166', NULL, N'Varna', N'Varna', N'9000', N'BG', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 9)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (9, 5, N'Shipping', N'Pristanishtna zona', N'Skladova baza 12', N'Varna', N'Varna', N'9000', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 6: Budapest Supply Kft. (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 10)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (10, 6, N'Both', N'Andrassy ut 60', NULL, N'Budapest', N'Pest', N'1062', N'HU', 1, '2026-04-01T00:00:00', NULL);

-- Customer 7: Burgas Retail Store EOOD (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 11)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (11, 7, N'Both', N'ul. Aleksandrovska 73', NULL, N'Burgas', N'Burgas', N'8000', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 8: Ruse Market OOD (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 12)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (12, 8, N'Both', N'ul. Borisova 12', NULL, N'Ruse', N'Ruse', N'7000', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 9: Stara Zagora Supermarket AD (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 13)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (13, 9, N'Both', N'bul. Tsar Simeon Veliki 100', NULL, N'Stara Zagora', N'Stara Zagora', N'6000', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 10: Wien Handels AG (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 14)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (14, 10, N'Both', N'Mariahilfer Strasse 88', N'Stock 2', N'Wien', N'Wien', N'1070', N'AT', 1, '2026-04-01T00:00:00', NULL);

-- Customer 11: Thessaloniki Shop SA (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 15)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (15, 11, N'Both', N'Odos Tsimiski 43', NULL, N'Thessaloniki', N'Central Macedonia', N'54623', N'GR', 1, '2026-04-01T00:00:00', NULL);

-- Customer 13: Balkan Distribution OOD (2 addresses)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 16)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (16, 13, N'Billing', N'bul. Tsarigradsko shose 115', N'Biznes park Sofia, Sgrada 7', N'Sofia', N'Sofia-grad', N'1784', N'BG', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 17)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (17, 13, N'Shipping', N'bul. Evropa 178', N'Logistichen tsentar 3', N'Sofia', N'Sofia-grad', N'1330', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 14: Praha Distribution s.r.o. (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 18)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (18, 14, N'Both', N'Vinohradska 2828', NULL, N'Praha', N'Praha', N'12000', N'CZ', 1, '2026-04-01T00:00:00', NULL);

-- Customer 15: Warszawa Logistics (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 19)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (19, 15, N'Both', N'ul. Marszalkowska 84', N'Piertro 5', N'Warszawa', N'Mazowieckie', N'00-514', N'PL', 1, '2026-04-01T00:00:00', NULL);

-- Customer 17: Bratislava Partners s.r.o. (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 20)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (20, 17, N'Both', N'Obchodna 52', NULL, N'Bratislava', N'Bratislavsky', N'81106', N'SK', 1, '2026-04-01T00:00:00', NULL);

-- Customer 18: Veliko Tarnovo Supply AD (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 21)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (21, 18, N'Both', N'ul. Nikola Gabrovski 22', NULL, N'Veliko Tarnovo', N'Veliko Tarnovo', N'5000', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 19: Pernik Manufacturing OOD (2 addresses)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 22)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (22, 19, N'Billing', N'ul. Moshino 5', NULL, N'Pernik', N'Pernik', N'2300', N'BG', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 23)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (23, 19, N'Shipping', N'Industrialen kvartal Izok', N'Hala 14', N'Pernik', N'Pernik', N'2300', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 20: Milano Produzione S.r.l. (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 24)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (24, 20, N'Both', N'Via Torino 72', NULL, N'Milano', N'Lombardia', N'20123', N'IT', 1, '2026-04-01T00:00:00', NULL);

-- Customer 22: Hamburg Werke GmbH (2 addresses)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 25)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (25, 22, N'Billing', N'Ballindamm 40', NULL, N'Hamburg', N'Hamburg', N'20095', N'DE', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 26)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (26, 22, N'Shipping', N'Suederstrasse 310', N'Halle B', N'Hamburg', N'Hamburg', N'20537', N'DE', 1, '2026-04-01T00:00:00', NULL);

-- Customer 23: Kazanlak Rose Products AD (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 27)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (27, 23, N'Both', N'bul. Rozova dolina 1', NULL, N'Kazanlak', N'Stara Zagora', N'6100', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 25: Sofia Freight Services EOOD (2 addresses)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 28)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (28, 25, N'Billing', N'ul. Pozitano 7', NULL, N'Sofia', N'Sofia-grad', N'1000', N'BG', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 29)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (29, 25, N'Shipping', N'Letishte Sofia Kargo', N'Terminal 2', N'Sofia', N'Sofia-grad', N'1540', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 26: Balkan IT Solutions OOD (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 30)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (30, 26, N'Both', N'bul. Aleksandar Malinov 31', N'Biznes tsentar Obelia', N'Sofia', N'Sofia-grad', N'1729', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 28: Athens Consulting SA (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 31)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (31, 28, N'Both', N'Leoforos Vasilissis Sofias 68', NULL, N'Athens', N'Attica', N'11528', N'GR', 1, '2026-04-01T00:00:00', NULL);

-- Customer 30: Haskovo Maintenance OOD (1 address)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 32)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (32, 30, N'Both', N'ul. San Stefano 18', NULL, N'Haskovo', N'Haskovo', N'6300', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Additional addresses for variety
-- Customer 12: Blagoevgrad Commerce EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 33)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (33, 12, N'Both', N'ul. Todor Aleksandrov 41', NULL, N'Blagoevgrad', N'Blagoevgrad', N'2700', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 16: Gabrovo Express EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 34)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (34, 16, N'Both', N'ul. Opalchenska 3', NULL, N'Gabrovo', N'Gabrovo', N'5300', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 21: Pleven Industrial EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 35)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (35, 21, N'Both', N'ul. Vasil Levski 150', NULL, N'Pleven', N'Pleven', N'5800', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Customer 27: Dobrich Transport AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 36)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (36, 27, N'Both', N'ul. 25-ti Septemvri 54', NULL, N'Dobrich', N'Dobrich', N'9300', N'BG', 1, '2026-04-01T00:00:00', NULL);

-- Additional Shipping addresses for customers with Billing only
-- Customer 9: Stara Zagora Supermarket (Shipping)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 37)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (37, 9, N'Shipping', N'Industrialna zona Zapad', N'Skladov komplex', N'Stara Zagora', N'Stara Zagora', N'6006', N'BG', 0, '2026-04-01T00:00:00', NULL);

-- Customer 7: Burgas Retail Store (Shipping warehouse)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 38)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (38, 7, N'Shipping', N'Severnata industrialna zona', N'Sklad 4B', N'Burgas', N'Burgas', N'8016', N'BG', 0, '2026-04-01T00:00:00', NULL);

-- Customer 8: Ruse Market OOD (Shipping)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 39)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (39, 8, N'Shipping', N'Industrialna zona Zapad', NULL, N'Ruse', N'Ruse', N'7009', N'BG', 0, '2026-04-01T00:00:00', NULL);

-- Customer 26: Balkan IT Solutions OOD (Shipping/datacenter)
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerAddresses] WHERE [Id] = 40)
    INSERT INTO [customers].[CustomerAddresses] ([Id], [CustomerId], [AddressType], [StreetLine1], [StreetLine2], [City], [StateProvince], [PostalCode], [CountryCode], [IsDefault], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (40, 26, N'Shipping', N'bul. Botevgradsko shose 247', N'Data Center Sofia 1', N'Sofia', N'Sofia-grad', N'1517', N'BG', 0, '2026-04-01T00:00:00', NULL);

SET IDENTITY_INSERT [customers].[CustomerAddresses] OFF;
");
        }

        private static void SeedCustomerPhones(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [customers].[CustomerPhones] ON;

-- Customer 1: Sofia Trading OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 1)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (1, 1, N'Landline', N'+359 2 981 7600', N'101', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 2)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (2, 1, N'Mobile', N'+359 88 765 4321', NULL, 0, '2026-04-01T00:00:00', NULL);

-- Customer 2: Plovdiv Logistics EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 3)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (3, 2, N'Mobile', N'+359 89 234 5678', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 3: Berlin Import GmbH
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 4)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (4, 3, N'Landline', N'+49 30 2345 6789', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 4: Bucharest Electronics SRL
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 5)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (5, 4, N'Landline', N'+40 21 312 4567', N'200', 1, '2026-04-01T00:00:00', NULL);

-- Customer 5: Varna Wholesale Group AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 6)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (6, 5, N'Landline', N'+359 52 600 123', NULL, 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 7)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (7, 5, N'Fax', N'+359 52 600 124', NULL, 0, '2026-04-01T00:00:00', NULL);

-- Customer 7: Burgas Retail Store EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 8)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (8, 7, N'Mobile', N'+359 87 654 3210', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 8: Ruse Market OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 9)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (9, 8, N'Landline', N'+359 82 820 456', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 9: Stara Zagora Supermarket AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 10)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (10, 9, N'Landline', N'+359 42 620 789', N'10', 1, '2026-04-01T00:00:00', NULL);

-- Customer 11: Thessaloniki Shop SA
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 11)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (11, 11, N'Landline', N'+30 2310 555 123', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 13: Balkan Distribution OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 12)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (12, 13, N'Landline', N'+359 2 974 3200', N'300', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 13)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (13, 13, N'Mobile', N'+359 88 123 4567', NULL, 0, '2026-04-01T00:00:00', NULL);

-- Customer 14: Praha Distribution s.r.o.
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 14)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (14, 14, N'Landline', N'+420 221 456 789', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 15: Warszawa Logistics
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 15)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (15, 15, N'Landline', N'+48 22 456 78 90', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 18: Veliko Tarnovo Supply AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 16)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (16, 18, N'Mobile', N'+359 89 876 5432', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 19: Pernik Manufacturing OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 17)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (17, 19, N'Landline', N'+359 76 601 234', N'50', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 18)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (18, 19, N'Fax', N'+359 76 601 235', NULL, 0, '2026-04-01T00:00:00', NULL);

-- Customer 20: Milano Produzione S.r.l.
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 19)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (19, 20, N'Landline', N'+39 02 7654 3210', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 22: Hamburg Werke GmbH
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 20)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (20, 22, N'Landline', N'+49 40 3456 7890', N'110', 1, '2026-04-01T00:00:00', NULL);

-- Customer 23: Kazanlak Rose Products AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 21)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (21, 23, N'Landline', N'+359 431 64 200', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 25: Sofia Freight Services EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 22)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (22, 25, N'Landline', N'+359 2 805 5500', NULL, 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 23)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (23, 25, N'Mobile', N'+359 87 555 0000', NULL, 0, '2026-04-01T00:00:00', NULL);

-- Customer 26: Balkan IT Solutions OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 24)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (24, 26, N'Mobile', N'+359 88 900 1234', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 28: Athens Consulting SA
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 25)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (25, 28, N'Landline', N'+30 210 723 4567', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 30: Haskovo Maintenance OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 26)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (26, 30, N'Mobile', N'+359 88 300 4567', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 6: Budapest Supply Kft.
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 27)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (27, 6, N'Landline', N'+36 1 266 7890', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 17: Bratislava Partners s.r.o.
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 28)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (28, 17, N'Landline', N'+421 2 5443 2100', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 10: Wien Handels AG
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 29)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (29, 10, N'Landline', N'+43 1 512 3456', NULL, 1, '2026-04-01T00:00:00', NULL);

-- Customer 12: Blagoevgrad Commerce EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerPhones] WHERE [Id] = 30)
    INSERT INTO [customers].[CustomerPhones] ([Id], [CustomerId], [PhoneType], [PhoneNumber], [Extension], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (30, 12, N'Mobile', N'+359 89 456 7890', NULL, 1, '2026-04-01T00:00:00', NULL);

SET IDENTITY_INSERT [customers].[CustomerPhones] OFF;
");
        }

        private static void SeedCustomerEmails(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [customers].[CustomerEmails] ON;

-- Customer 1: Sofia Trading OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 1)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (1, 1, N'General', N'info@sofiatrading.bg', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 2)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (2, 1, N'Billing', N'billing@sofiatrading.bg', 0, '2026-04-01T00:00:00', NULL);

-- Customer 2: Plovdiv Logistics EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 3)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (3, 2, N'General', N'office@plovdivlogistics.bg', 1, '2026-04-01T00:00:00', NULL);

-- Customer 3: Berlin Import GmbH
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 4)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (4, 3, N'General', N'kontakt@berlinimport.de', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 5)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (5, 3, N'Billing', N'buchhaltung@berlinimport.de', 0, '2026-04-01T00:00:00', NULL);

-- Customer 4: Bucharest Electronics SRL
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 6)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (6, 4, N'General', N'contact@bucelectronics.ro', 1, '2026-04-01T00:00:00', NULL);

-- Customer 5: Varna Wholesale Group AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 7)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (7, 5, N'General', N'info@varnawholesale.bg', 1, '2026-04-01T00:00:00', NULL);

-- Customer 7: Burgas Retail Store EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 8)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (8, 7, N'General', N'shop@burgasretail.bg', 1, '2026-04-01T00:00:00', NULL);

-- Customer 8: Ruse Market OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 9)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (9, 8, N'General', N'info@rusemarket.bg', 1, '2026-04-01T00:00:00', NULL);

-- Customer 9: Stara Zagora Supermarket AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 10)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (10, 9, N'General', N'info@szsupermarket.bg', 1, '2026-04-01T00:00:00', NULL);

-- Customer 11: Thessaloniki Shop SA
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 11)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (11, 11, N'General', N'info@thessalonikishop.gr', 1, '2026-04-01T00:00:00', NULL);

-- Customer 13: Balkan Distribution OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 12)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (12, 13, N'General', N'info@balkandistribution.bg', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 13)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (13, 13, N'Billing', N'finance@balkandistribution.bg', 0, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 14)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (14, 13, N'Support', N'support@balkandistribution.bg', 0, '2026-04-01T00:00:00', NULL);

-- Customer 14: Praha Distribution s.r.o.
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 15)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (15, 14, N'General', N'info@prahadistribution.cz', 1, '2026-04-01T00:00:00', NULL);

-- Customer 15: Warszawa Logistics
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 16)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (16, 15, N'General', N'biuro@warszawalogistics.pl', 1, '2026-04-01T00:00:00', NULL);

-- Customer 19: Pernik Manufacturing OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 17)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (17, 19, N'General', N'office@pernikmanufacturing.bg', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 18)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (18, 19, N'Billing', N'billing@pernikmanufacturing.bg', 0, '2026-04-01T00:00:00', NULL);

-- Customer 20: Milano Produzione S.r.l.
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 19)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (19, 20, N'General', N'info@milanoproduzione.it', 1, '2026-04-01T00:00:00', NULL);

-- Customer 22: Hamburg Werke GmbH
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 20)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (20, 22, N'General', N'info@hamburgwerke.de', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 21)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (21, 22, N'Billing', N'rechnung@hamburgwerke.de', 0, '2026-04-01T00:00:00', NULL);

-- Customer 23: Kazanlak Rose Products AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 22)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (22, 23, N'General', N'info@kazanlakrose.bg', 1, '2026-04-01T00:00:00', NULL);

-- Customer 25: Sofia Freight Services EOOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 23)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (23, 25, N'General', N'office@sofiafreight.bg', 1, '2026-04-01T00:00:00', NULL);

IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 24)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (24, 25, N'Support', N'dispatch@sofiafreight.bg', 0, '2026-04-01T00:00:00', NULL);

-- Customer 26: Balkan IT Solutions OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 25)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (25, 26, N'General', N'hello@balkanit.bg', 1, '2026-04-01T00:00:00', NULL);

-- Customer 28: Athens Consulting SA
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 26)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (26, 28, N'General', N'info@athensconsulting.gr', 1, '2026-04-01T00:00:00', NULL);

-- Customer 30: Haskovo Maintenance OOD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 27)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (27, 30, N'General', N'info@haskovomaintenance.bg', 1, '2026-04-01T00:00:00', NULL);

-- Customer 6: Budapest Supply Kft.
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 28)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (28, 6, N'General', N'info@budapestsupply.hu', 1, '2026-04-01T00:00:00', NULL);

-- Customer 17: Bratislava Partners s.r.o.
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 29)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (29, 17, N'General', N'info@bratislavapartners.sk', 1, '2026-04-01T00:00:00', NULL);

-- Customer 18: Veliko Tarnovo Supply AD
IF NOT EXISTS (SELECT 1 FROM [customers].[CustomerEmails] WHERE [Id] = 30)
    INSERT INTO [customers].[CustomerEmails] ([Id], [CustomerId], [EmailType], [EmailAddress], [IsPrimary], [CreatedAtUtc], [ModifiedAtUtc])
    VALUES (30, 18, N'General', N'info@vtsupply.bg', 1, '2026-04-01T00:00:00', NULL);

SET IDENTITY_INSERT [customers].[CustomerEmails] OFF;
");
        }
    }
}
