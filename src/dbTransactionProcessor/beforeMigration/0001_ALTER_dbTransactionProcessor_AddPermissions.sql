-- Add database permissions for dbTransactionProcessor
-- This script runs before migrations

-- Create login at server level if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'TransactionProcessorUser')
BEGIN
    CREATE LOGIN [TransactionProcessorUser] WITH PASSWORD = 'P@ssw0rd123!';
END

-- Create database user if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'TransactionProcessorUser')
BEGIN
    CREATE USER [TransactionProcessorUser] FOR LOGIN [TransactionProcessorUser];
END

-- Grant necessary permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO [TransactionProcessorUser];
GRANT EXECUTE ON SCHEMA::dbo TO [TransactionProcessorUser];
