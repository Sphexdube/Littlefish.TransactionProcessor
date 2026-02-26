-- Tenants table
-- Stores multi-tenant configuration including per-tenant business rule thresholds
--
-- TransactionStatus enum values:
--   IsActive: 1 = active, 0 = inactive

CREATE TABLE [dbo].[Tenants]
(
    [Id]                  UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Name]                NVARCHAR(200)     NOT NULL,
    [IsActive]            BIT               NOT NULL DEFAULT 1,
    [DailyMerchantLimit]  DECIMAL(18, 2)    NOT NULL DEFAULT 100000.00,
    [HighValueThreshold]  DECIMAL(18, 2)    NOT NULL DEFAULT 10000.00,
    [CreatedAt]           DATETIMEOFFSET    NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [UpdatedAt]           DATETIMEOFFSET    NULL,

    CONSTRAINT [PK_Tenants] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Tenants_Name] UNIQUE ([Name])
);
GO
