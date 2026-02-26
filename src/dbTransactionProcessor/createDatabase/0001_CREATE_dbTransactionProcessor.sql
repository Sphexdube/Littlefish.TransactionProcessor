IF (NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE (name = 'dbTransactionProcessor')))
BEGIN
    DECLARE @FilePath VARCHAR(300), @LogFilePath VARCHAR(300), @FolderPrefix VARCHAR(300)
    SELECT @FilePath = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS VARCHAR(300))
    SELECT @LogFilePath = CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS VARCHAR(300))
    SELECT @FolderPrefix = 'dbTransactionProcessor\'

    SELECT @LogFilePath = REPLACE(@FilePath, '\Data1', '\Log1')

    EXEC (N'CREATE DATABASE [dbTransactionProcessor]
        ON PRIMARY ( NAME = N''dbTransactionProcessor'', FILENAME = N''' + @FilePath + @FolderPrefix + 'dbTransactionProcessor.mdf'' , SIZE = 250MB , MAXSIZE = UNLIMITED, FILEGROWTH = 250MB )
        LOG ON ( NAME = N''dbTransactionProcessor_log'', FILENAME = N''' + @LogFilePath + @FolderPrefix + 'dbTransactionProcessor.ldf'' , SIZE = 250MB , MAXSIZE = UNLIMITED , FILEGROWTH = 250MB )
            WITH CATALOG_COLLATION = DATABASE_DEFAULT')

    IF SERVERPROPERTY('EngineEdition') <> 5
    BEGIN
        ALTER DATABASE [dbTransactionProcessor] SET AUTO_CLOSE OFF WITH NO_WAIT;

        ALTER DATABASE [dbTransactionProcessor] SET RECOVERY SIMPLE WITH NO_WAIT;

        ALTER DATABASE [dbTransactionProcessor] SET READ_COMMITTED_SNAPSHOT ON;

        IF (@@servername LIKE '%PRD%')
        BEGIN
            ALTER DATABASE [dbTransactionProcessor] SET RECOVERY FULL WITH NO_WAIT;
        END
    END
END
