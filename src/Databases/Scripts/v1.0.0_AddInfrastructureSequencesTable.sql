-- =============================================================================
-- Migration: v1.0.0_AddInfrastructureSequencesTable.sql
-- SDD Reference: SDD-INFRA-003 — Centralized Sequence Generation
-- Description: Creates the infrastructure.Sequences table for centralized
--              sequence/number generation with row-level locking.
-- Applies To: All databases that use ISequenceGenerator
--             (Auth, Customers, Inventory, Purchasing, Fulfillment, EventLog)
-- =============================================================================

-- Schema
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'infrastructure')
    EXEC('CREATE SCHEMA [infrastructure]');

-- Table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('infrastructure.Sequences') AND type = 'U')
CREATE TABLE [infrastructure].[Sequences]
(
    [Id]            INT             IDENTITY(1,1)   NOT NULL,
    [CompositeKey]  NVARCHAR(200)   NOT NULL,
    [SequenceKey]   NVARCHAR(50)    NOT NULL,
    [CurrentValue]  INT             NOT NULL    CONSTRAINT DF_Sequences_CurrentValue DEFAULT (0),
    [ResetPolicy]   NVARCHAR(20)    NOT NULL,
    [CreatedAtUtc]  DATETIME2(7)    NOT NULL    CONSTRAINT DF_Sequences_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    [ModifiedAtUtc] DATETIME2(7)    NOT NULL    CONSTRAINT DF_Sequences_ModifiedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Sequences] PRIMARY KEY CLUSTERED ([Id])
);

-- Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sequences_CompositeKey' AND object_id = OBJECT_ID('infrastructure.Sequences'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Sequences_CompositeKey] ON [infrastructure].[Sequences] ([CompositeKey]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sequences_SequenceKey' AND object_id = OBJECT_ID('infrastructure.Sequences'))
    CREATE NONCLUSTERED INDEX [IX_Sequences_SequenceKey] ON [infrastructure].[Sequences] ([SequenceKey]);
