-- Seed tenants for local development
-- DailyMerchantLimit: R100,000 per merchant per day
-- HighValueThreshold: R10,000 per transaction triggers manual review

INSERT INTO [dbo].[Tenants] ([Id], [Name], [IsActive], [DailyMerchantLimit], [HighValueThreshold], [CreatedAt])
VALUES
    ('617b6baa-2be4-4e29-984e-a463ea060c47', 'Acme Corporation',  1, 100000.00, 10000.00, SYSDATETIMEOFFSET()),
    ('576c6ade-211f-425b-ac8f-4f87e4e6e7f7', 'Beta Retail Group', 1,  50000.00,  5000.00, SYSDATETIMEOFFSET()),
    ('faab094e-a712-4416-91ac-fec7c0cf8da5', 'Gamma Finance',     1, 250000.00, 25000.00, SYSDATETIMEOFFSET());
