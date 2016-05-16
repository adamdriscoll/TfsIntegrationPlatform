USE [Tfs_IntegrationPlatform]

GO
/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed before the build script	
 Use SQLCMD syntax to include a file into the pre-deployment script			
 Example:      :r .\filename.sql								
 Use SQLCMD syntax to reference a variable in the pre-deployment script		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

GO

GO
PRINT N'Dropping FriendlyName...';


GO
EXECUTE sp_dropextendedproperty @name = N'FriendlyName';


GO
PRINT N'Dropping ReferenceName...';


GO
EXECUTE sp_dropextendedproperty @name = N'ReferenceName';


GO
PRINT N'Dropping dbo.FK_SessionConfigurations3...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS] DROP CONSTRAINT [FK_SessionConfigurations3];


GO
PRINT N'Dropping dbo.FK_SessionConfigurations2...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS] DROP CONSTRAINT [FK_SessionConfigurations2];


GO
PRINT N'Dropping dbo.FK_MigrationSourceConfigs...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCE_CONFIGS] DROP CONSTRAINT [FK_MigrationSourceConfigs];


GO
PRINT N'Altering dbo.FILTER_ITEM_PAIR...';


GO
ALTER TABLE [dbo].[FILTER_ITEM_PAIR] ALTER COLUMN [Filter1] NVARCHAR (4000) NULL;

ALTER TABLE [dbo].[FILTER_ITEM_PAIR] ALTER COLUMN [Filter2] NVARCHAR (4000) NULL;


GO
PRINT N'Starting rebuilding table dbo.MIGRATION_SOURCE_CONFIGS...';


GO
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;


GO
BEGIN TRANSACTION;

CREATE TABLE [dbo].[tmp_ms_xx_MIGRATION_SOURCE_CONFIGS] (
    [Id]                INT      IDENTITY (1, 1) NOT NULL,
    [CreationTime]      DATETIME NOT NULL,
    [MigrationSourceId] INT      NOT NULL,
    [GeneralSettingXml] XML      NULL,
    [SettingXml]        XML      NULL,
    [SettingXmlSchema]  XML      NULL
);

ALTER TABLE [dbo].[tmp_ms_xx_MIGRATION_SOURCE_CONFIGS]
    ADD CONSTRAINT [tmp_ms_xx_clusteredindex_PK_MigrationSourceConfigs] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);

IF EXISTS (SELECT TOP 1 1
           FROM   [dbo].[MIGRATION_SOURCE_CONFIGS])
    BEGIN
        SET IDENTITY_INSERT [dbo].[tmp_ms_xx_MIGRATION_SOURCE_CONFIGS] ON;
        INSERT INTO [dbo].[tmp_ms_xx_MIGRATION_SOURCE_CONFIGS] ([Id], [CreationTime], [MigrationSourceId], [SettingXml], [SettingXmlSchema])
        SELECT   [Id],
                 [CreationTime],
                 [MigrationSourceId],
                 [SettingXml],
                 [SettingXmlSchema]
        FROM     [dbo].[MIGRATION_SOURCE_CONFIGS]
        ORDER BY [Id] ASC;
        SET IDENTITY_INSERT [dbo].[tmp_ms_xx_MIGRATION_SOURCE_CONFIGS] OFF;
    END

DROP TABLE [dbo].[MIGRATION_SOURCE_CONFIGS];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_MIGRATION_SOURCE_CONFIGS]', N'MIGRATION_SOURCE_CONFIGS';

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_clusteredindex_PK_MigrationSourceConfigs]', N'PK_MigrationSourceConfigs', N'OBJECT';

COMMIT TRANSACTION;


GO
PRINT N'Altering dbo.SESSION_GROUP_CONFIGS...';


GO
ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS]
    ADD [UserIdentityMappingsConfig] XML NULL;


GO
PRINT N'Creating dbo.ADDINS...';


GO
CREATE TABLE [dbo].[ADDINS] (
    [Id]              INT              IDENTITY (1, 1) NOT NULL,
    [ReferenceName]   UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]    NVARCHAR (128)   NOT NULL,
    [ProviderVersion] NVARCHAR (30)    NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating dbo.SYNC_POINT...';


GO
CREATE TABLE [dbo].[SYNC_POINT] (
    [Id]                            BIGINT           IDENTITY (1, 1) NOT NULL,
    [SessionUniqueId]               UNIQUEIDENTIFIER NOT NULL,
    [SourceUniqueId]                UNIQUEIDENTIFIER NOT NULL,
    [SourceHighWaterMarkName]       NVARCHAR (50)    NOT NULL,
    [SourceHighWaterMarkValue]      NVARCHAR (50)    NULL,
    [LastMigratedTargetItemId]      NVARCHAR (300)   NOT NULL,
    [LastMigratedTargetItemVersion] NVARCHAR (50)    NOT NULL
);


GO
PRINT N'Creating dbo.UK_MigrationSourceConfigs...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCE_CONFIGS]
    ADD CONSTRAINT [UK_MigrationSourceConfigs] UNIQUE NONCLUSTERED ([CreationTime] ASC, [MigrationSourceId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating dbo.PK_SyncPoint...';


GO
ALTER TABLE [dbo].[SYNC_POINT]
    ADD CONSTRAINT [PK_SyncPoint] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating dbo.FK_SessionConfigurations3...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS]
    ADD CONSTRAINT [FK_SessionConfigurations3] FOREIGN KEY ([RightSourceConfigId]) REFERENCES [dbo].[MIGRATION_SOURCE_CONFIGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating dbo.FK_SessionConfigurations2...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS]
    ADD CONSTRAINT [FK_SessionConfigurations2] FOREIGN KEY ([LeftSourceConfigId]) REFERENCES [dbo].[MIGRATION_SOURCE_CONFIGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating dbo.FK_MigrationSourceConfigs...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCE_CONFIGS]
    ADD CONSTRAINT [FK_MigrationSourceConfigs] FOREIGN KEY ([MigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Altering dbo.prc_LoadChangeAction...';


GO
ALTER PROCEDURE [dbo].[prc_LoadChangeAction]
@ChangeGroupID BIGINT
AS
SELECT * FROM [dbo].[RUNTIME_CHANGE_ACTION]
WHERE    ChangeGroupId = @ChangeGroupID
ORDER BY [FromPath] ASC


GO
PRINT N'Creating FriendlyName...';


GO
EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v1.4';


GO
PRINT N'Creating ReferenceName...';


GO
EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = '0023D2ED-91D4-46D8-ABB3-243127646196';

GO
