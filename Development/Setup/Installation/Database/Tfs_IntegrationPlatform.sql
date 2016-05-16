/*
Deployment script for Tfs_IntegrationPlatform
*/

GO
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;

SET NUMERIC_ROUNDABORT OFF;


GO
USE [master]

GO
IF (DB_ID(N'Tfs_IntegrationPlatform') IS NOT NULL
    AND DATABASEPROPERTYEX(N'Tfs_IntegrationPlatform','Status') <> N'ONLINE')
BEGIN
    RAISERROR(N'The state of the target database, %s, is not set to ONLINE. To deploy to this database, its state must be set to ONLINE.', 16, 127,N'Tfs_IntegrationPlatform') WITH NOWAIT
    RETURN
END

GO
IF (DB_ID(N'Tfs_IntegrationPlatform') IS NOT NULL) 
BEGIN
    ALTER DATABASE [Tfs_IntegrationPlatform]
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [Tfs_IntegrationPlatform];
END

GO
PRINT N'Creating Tfs_IntegrationPlatform...'
GO
CREATE DATABASE [Tfs_IntegrationPlatform] COLLATE SQL_Latin1_General_CP1_CS_AS
GO
EXECUTE sp_dbcmptlevel [Tfs_IntegrationPlatform], 90;


GO
IF EXISTS (SELECT 1
           FROM   [master].[dbo].[sysdatabases]
           WHERE  [name] = N'Tfs_IntegrationPlatform')
    BEGIN
        ALTER DATABASE [Tfs_IntegrationPlatform]
            SET ANSI_NULLS ON,
                ANSI_PADDING ON,
                ANSI_WARNINGS ON,
                ARITHABORT ON,
                CONCAT_NULL_YIELDS_NULL ON,
                NUMERIC_ROUNDABORT OFF,
                QUOTED_IDENTIFIER ON,
                ANSI_NULL_DEFAULT ON,
                CURSOR_DEFAULT LOCAL,
                RECOVERY FULL,
                CURSOR_CLOSE_ON_COMMIT OFF,
                AUTO_CREATE_STATISTICS ON,
                AUTO_SHRINK OFF,
                AUTO_UPDATE_STATISTICS ON,
                RECURSIVE_TRIGGERS OFF 
            WITH ROLLBACK IMMEDIATE;
        ALTER DATABASE [Tfs_IntegrationPlatform]
            SET AUTO_CLOSE OFF 
            WITH ROLLBACK IMMEDIATE;
    END


GO
IF EXISTS (SELECT 1
           FROM   [master].[dbo].[sysdatabases]
           WHERE  [name] = N'Tfs_IntegrationPlatform')
    BEGIN
        ALTER DATABASE [Tfs_IntegrationPlatform]
            SET ALLOW_SNAPSHOT_ISOLATION OFF;
    END


GO
IF EXISTS (SELECT 1
           FROM   [master].[dbo].[sysdatabases]
           WHERE  [name] = N'Tfs_IntegrationPlatform')
    BEGIN
        ALTER DATABASE [Tfs_IntegrationPlatform]
            SET READ_COMMITTED_SNAPSHOT OFF;
    END


GO
IF EXISTS (SELECT 1
           FROM   [master].[dbo].[sysdatabases]
           WHERE  [name] = N'Tfs_IntegrationPlatform')
    BEGIN
        ALTER DATABASE [Tfs_IntegrationPlatform]
            SET AUTO_UPDATE_STATISTICS_ASYNC ON,
                PAGE_VERIFY NONE,
                DATE_CORRELATION_OPTIMIZATION OFF,
                DISABLE_BROKER,
                PARAMETERIZATION SIMPLE,
                SUPPLEMENTAL_LOGGING OFF 
            WITH ROLLBACK IMMEDIATE;
    END


GO
IF IS_SRVROLEMEMBER(N'sysadmin') = 1
    BEGIN
        IF EXISTS (SELECT 1
                   FROM   [master].[dbo].[sysdatabases]
                   WHERE  [name] = N'Tfs_IntegrationPlatform')
            BEGIN
                EXECUTE sp_executesql N'ALTER DATABASE [Tfs_IntegrationPlatform]
    SET TRUSTWORTHY OFF,
        DB_CHAINING OFF 
    WITH ROLLBACK IMMEDIATE';
            END
    END
ELSE
    BEGIN
        PRINT N'The database settings cannot be modified. You must be a SysAdmin to apply these settings.';
    END


GO
USE [Tfs_IntegrationPlatform]

GO
IF fulltextserviceproperty(N'IsFulltextInstalled') = 1
    EXECUTE sp_fulltext_database 'disable';


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
PRINT N'Creating [dbo].[ADDINS]...';


GO
CREATE TABLE [dbo].[ADDINS] (
    [Id]              INT              IDENTITY (1, 1) NOT NULL,
    [ReferenceName]   UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]    NVARCHAR (128)   NOT NULL,
    [ProviderVersion] NVARCHAR (30)    NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[CONFIG_CHECKOUT_RECORDS]...';


GO
CREATE TABLE [dbo].[CONFIG_CHECKOUT_RECORDS] (
    [SessionGroupConfigId] INT              NOT NULL,
    [CheckOutToken]        UNIQUEIDENTIFIER NULL
);


GO
PRINT N'Creating PK_ConfigCheckoutRecords...';


GO
ALTER TABLE [dbo].[CONFIG_CHECKOUT_RECORDS]
    ADD CONSTRAINT [PK_ConfigCheckoutRecords] PRIMARY KEY CLUSTERED ([SessionGroupConfigId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[CONFLICT_CONFLICT_TYPES]...';


GO
CREATE TABLE [dbo].[CONFLICT_CONFLICT_TYPES] (
    [Id]             INT              IDENTITY (1, 1) NOT NULL,
    [ReferenceName]  UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]   NVARCHAR (300)   NOT NULL,
    [DescriptionDoc] NVARCHAR (MAX)   NULL,
    [IsActive]       BIT              NULL,
    [ProviderId]     INT              NULL
);


GO
PRINT N'Creating PK_CoflictTypes...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICT_TYPES]
    ADD CONSTRAINT [PK_CoflictTypes] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[CONFLICT_CONFLICTS]...';


GO
CREATE TABLE [dbo].[CONFLICT_CONFLICTS] (
    [Id]                           INT              IDENTITY (1, 1) NOT NULL,
    [ConflictListId]               INT              NOT NULL,
    [ConflictTypeId]               INT              NOT NULL,
    [ConflictDetails]              NVARCHAR (MAX)   NULL,
    [ConflictedChangeActionId]     BIGINT           NULL,
    [ChangeGroupId]                BIGINT           NULL,
    [ConflictedLinkChangeActionId] BIGINT           NULL,
    [ConflictedLinkChangeGroupId]  BIGINT           NULL,
    [ScopeId]                      UNIQUEIDENTIFIER NOT NULL,
    [SourceMigrationSourceId]      INT              NULL,
    [ScopeHint]                    NVARCHAR (MAX)   NULL,
    [Status]                       INT              NOT NULL,
    [ConflictCount]                INT              NULL,
    [ResolvedByRuleId]             INT              NULL,
    [CreationTime]                 DATETIME         NULL
);


GO
PRINT N'Creating PK_Conflicts...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
    ADD CONSTRAINT [PK_Conflicts] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[CONFLICT_CONTENT_RESV]...';


GO
CREATE TABLE [dbo].[CONFLICT_CONTENT_RESV] (
    [ConflictId] INT            NOT NULL,
    [ItemId]     BIGINT         NOT NULL,
    [Content]    NVARCHAR (MAX) NULL
);


GO
PRINT N'Creating PK_ContentResv...';


GO
ALTER TABLE [dbo].[CONFLICT_CONTENT_RESV]
    ADD CONSTRAINT [PK_ContentResv] PRIMARY KEY CLUSTERED ([ConflictId] ASC, [ItemId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[CONFLICT_RESOLUTION_ACTIONS]...';


GO
CREATE TABLE [dbo].[CONFLICT_RESOLUTION_ACTIONS] (
    [Id]            INT              IDENTITY (1, 1) NOT NULL,
    [ReferenceName] UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]  NVARCHAR (300)   NOT NULL,
    [IsActive]      BIT              NULL,
    [ProviderId]    INT              NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[CONFLICT_RESOLUTION_RULES]...';


GO
CREATE TABLE [dbo].[CONFLICT_RESOLUTION_RULES] (
    [Id]                 INT              IDENTITY (1, 1) NOT NULL,
    [ReferenceName]      UNIQUEIDENTIFIER NOT NULL,
    [ConflictTypeId]     INT              NOT NULL,
    [ResolutionActionId] INT              NOT NULL,
    [ScopeId]            INT              NOT NULL,
    [ScopeInfoUniqueId]  UNIQUEIDENTIFIER NOT NULL,
    [SourceInfoUniqueId] UNIQUEIDENTIFIER NOT NULL,
    [RuleData]           XML              NOT NULL,
    [CreationTime]       DATETIME         NOT NULL,
    [DeprecationTime]    DATETIME         NULL,
    [Status]             INT              NOT NULL
);


GO
PRINT N'Creating PK_ResolutionRules...';


GO
ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES]
    ADD CONSTRAINT [PK_ResolutionRules] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_ResolutionRules...';


GO
ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES]
    ADD CONSTRAINT [UK_ResolutionRules] UNIQUE NONCLUSTERED ([ReferenceName] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[CONFLICT_RULE_SCOPES]...';


GO
CREATE TABLE [dbo].[CONFLICT_RULE_SCOPES] (
    [Id]    INT            IDENTITY (1, 1) NOT NULL,
    [Scope] NVARCHAR (MAX) NOT NULL
);


GO
PRINT N'Creating PK_RuleScopes...';


GO
ALTER TABLE [dbo].[CONFLICT_RULE_SCOPES]
    ADD CONSTRAINT [PK_RuleScopes] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[EVENT_SINK]...';


GO
CREATE TABLE [dbo].[EVENT_SINK] (
    [Id]               INT            IDENTITY (1, 1) NOT NULL,
    [FriendlyName]     NVARCHAR (128) NOT NULL,
    [ProviderId]       INT            NOT NULL,
    [CreationTime]     DATETIME       NOT NULL,
    [SettingXml]       XML            NULL,
    [SettingXmlSchema] XML            NULL
);


GO
PRINT N'Creating PK_EventSink...';


GO
ALTER TABLE [dbo].[EVENT_SINK]
    ADD CONSTRAINT [PK_EventSink] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_EventSink...';


GO
ALTER TABLE [dbo].[EVENT_SINK]
    ADD CONSTRAINT [UK_EventSink] UNIQUE NONCLUSTERED ([ProviderId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[EVENT_SINK_JUNC]...';


GO
CREATE TABLE [dbo].[EVENT_SINK_JUNC] (
    [EventSinkId]     INT NOT NULL,
    [SessionConfigId] INT NOT NULL
);


GO
PRINT N'Creating PK_EventSinkJunc...';


GO
ALTER TABLE [dbo].[EVENT_SINK_JUNC]
    ADD CONSTRAINT [PK_EventSinkJunc] PRIMARY KEY CLUSTERED ([EventSinkId] ASC, [SessionConfigId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[FIELD_EXCLUSION_COLLECTION]...';


GO
CREATE TABLE [dbo].[FIELD_EXCLUSION_COLLECTION] (
    [FieldName]   NVARCHAR (200) NOT NULL,
    [Direction]   INT            NOT NULL,
    [WITypeMapId] INT            NOT NULL
);


GO
PRINT N'Creating PK_FieldExclCollection...';


GO
ALTER TABLE [dbo].[FIELD_EXCLUSION_COLLECTION]
    ADD CONSTRAINT [PK_FieldExclCollection] PRIMARY KEY CLUSTERED ([FieldName] ASC, [Direction] ASC, [WITypeMapId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[FIELD_MAP_COLLECTION]...';


GO
CREATE TABLE [dbo].[FIELD_MAP_COLLECTION] (
    [Id]             INT IDENTITY (1, 1) NOT NULL,
    [WITypeMapId]    INT NOT NULL,
    [ValueMappingId] INT NOT NULL,
    [Direction]      INT NOT NULL
);


GO
PRINT N'Creating PK_FieldMapCollection...';


GO
ALTER TABLE [dbo].[FIELD_MAP_COLLECTION]
    ADD CONSTRAINT [PK_FieldMapCollection] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_FieldMapCollection...';


GO
ALTER TABLE [dbo].[FIELD_MAP_COLLECTION]
    ADD CONSTRAINT [UK_FieldMapCollection] UNIQUE NONCLUSTERED ([WITypeMapId] ASC, [ValueMappingId] ASC, [Direction] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[FILTER_ITEM_PAIR]...';


GO
CREATE TABLE [dbo].[FILTER_ITEM_PAIR] (
    [Id]                                  INT              IDENTITY (1, 1) NOT NULL,
    [Filter1MigrationSourceReferenceName] UNIQUEIDENTIFIER NULL,
    [Filter1]                             NVARCHAR (4000)  NULL,
    [Filter1SnapshotPoint]                NVARCHAR (200)   NULL,
    [Filter1PeerSnapshotPoint]            NVARCHAR (200)   NULL,
    [Filter1MergeScope]                   NVARCHAR (200)   NULL,
    [Filter2MigrationSourceReferenceName] UNIQUEIDENTIFIER NULL,
    [Filter2]                             NVARCHAR (4000)  NULL,
    [Filter2SnapshotPoint]                NVARCHAR (200)   NULL,
    [Filter2PeerSnapshotPoint]            NVARCHAR (200)   NULL,
    [Filter2MergeScope]                   NVARCHAR (200)   NULL,
    [Neglect]                             BIT              NOT NULL,
    [SessionConfigId]                     INT              NOT NULL
);


GO
PRINT N'Creating PK_FilterItemPair...';


GO
ALTER TABLE [dbo].[FILTER_ITEM_PAIR]
    ADD CONSTRAINT [PK_FilterItemPair] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[HIGH_WATER_MARK]...';


GO
CREATE TABLE [dbo].[HIGH_WATER_MARK] (
    [Id]              INT              IDENTITY (1, 1) NOT NULL,
    [SessionUniqueId] UNIQUEIDENTIFIER NOT NULL,
    [SourceUniqueId]  UNIQUEIDENTIFIER NOT NULL,
    [Name]            NVARCHAR (50)    NOT NULL,
    [Value]           NVARCHAR (50)    NULL
);


GO
PRINT N'Creating PK_HighWaterMark...';


GO
ALTER TABLE [dbo].[HIGH_WATER_MARK]
    ADD CONSTRAINT [PK_HighWaterMark] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[LAST_PROCESSED_ITEM_VERSIONS]...';


GO
CREATE TABLE [dbo].[LAST_PROCESSED_ITEM_VERSIONS] (
    [MigrationSourceId] UNIQUEIDENTIFIER NOT NULL,
    [ItemId]            NVARCHAR (200)   NOT NULL,
    [Version]           NVARCHAR (200)   NOT NULL
);


GO
PRINT N'Creating PK_LastProcessedItemVersions...';


GO
ALTER TABLE [dbo].[LAST_PROCESSED_ITEM_VERSIONS]
    ADD CONSTRAINT [PK_LastProcessedItemVersions] PRIMARY KEY CLUSTERED ([MigrationSourceId] ASC, [ItemId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[LAST_PROCESSED_ITEM_VERSIONS].[Index_LastProcessedItemVersions]...';


GO
CREATE NONCLUSTERED INDEX [Index_LastProcessedItemVersions]
    ON [dbo].[LAST_PROCESSED_ITEM_VERSIONS]([ItemId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[LAST_PROCESSED_ITEM_VERSIONS_ASOF_CHANGEGROUPID]...';


GO
CREATE TABLE [dbo].[LAST_PROCESSED_ITEM_VERSIONS_ASOF_CHANGEGROUPID] (
    [MigrationSourceId] UNIQUEIDENTIFIER NOT NULL,
    [ChangeGroupId]     BIGINT           NOT NULL,
    PRIMARY KEY CLUSTERED ([MigrationSourceId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[LATENCY_POLL]...';


GO
CREATE TABLE [dbo].[LATENCY_POLL] (
    [Id]                 BIGINT         IDENTITY (1, 1) NOT NULL,
    [PollTime]           DATETIME       NOT NULL,
    [MigrationSourceId]  INT            NOT NULL,
    [MigrationHWM]       DATETIME       NOT NULL,
    [Latency]            INT            NOT NULL,
    [BacklogCount]       INT            NOT NULL,
    [LastMigratedChange] NVARCHAR (MAX) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[LINK_ARTIFACT_LINK]...';


GO
CREATE TABLE [dbo].[LINK_ARTIFACT_LINK] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [SourceArtifactUri] NVARCHAR (400) NOT NULL,
    [TargetArtifactUri] NVARCHAR (400) NOT NULL,
    [LinkTypeId]        INT            NOT NULL,
    [Comment]           NVARCHAR (MAX) NULL,
    [SourceArtifactId]  NVARCHAR (400) NULL,
    [IsLocked]          BIT            NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[LINK_ARTIFACT_TYPE]...';


GO
CREATE TABLE [dbo].[LINK_ARTIFACT_TYPE] (
    [Id]                  INT            IDENTITY (1, 1) NOT NULL,
    [ReferenceName]       NVARCHAR (200) NOT NULL,
    [DisplayName]         NVARCHAR (200) NOT NULL,
    [ArtifactContentType] NVARCHAR (400) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[LINK_LINK_CHANGE_ACTIONS]...';


GO
CREATE TABLE [dbo].[LINK_LINK_CHANGE_ACTIONS] (
    [Id]                   BIGINT           IDENTITY (1, 1) NOT NULL,
    [SessionGroupUniqueId] UNIQUEIDENTIFIER NOT NULL,
    [SessionUniqueId]      UNIQUEIDENTIFIER NOT NULL,
    [SourceId]             UNIQUEIDENTIFIER NOT NULL,
    [ActionId]             UNIQUEIDENTIFIER NOT NULL,
    [ArtifactLinkId]       INT              NOT NULL,
    [Status]               INT              NOT NULL,
    [LinkChangeGroupId]    BIGINT           NULL,
    [ExecutionOrder]       INT              NULL,
    [Conflicted]           BIT              NOT NULL,
    [ServerLinkChangeId]   NVARCHAR (300)   NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[LINK_LINK_CHANGE_GROUPS]...';


GO
CREATE TABLE [dbo].[LINK_LINK_CHANGE_GROUPS] (
    [Id]                       BIGINT           IDENTITY (1, 1) NOT NULL,
    [SessionGroupUniqueId]     UNIQUEIDENTIFIER NOT NULL,
    [SessionUniqueId]          UNIQUEIDENTIFIER NOT NULL,
    [SourceId]                 UNIQUEIDENTIFIER NOT NULL,
    [GroupName]                NVARCHAR (100)   NULL,
    [Status]                   INT              NOT NULL,
    [ContainsConflictedAction] BIT              NOT NULL,
    [Age]                      INT              NULL,
    [RetriesAtCurrAge]         INT              NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[LINK_LINK_TYPE]...';


GO
CREATE TABLE [dbo].[LINK_LINK_TYPE] (
    [Id]                   INT            IDENTITY (1, 1) NOT NULL,
    [ReferenceName]        NVARCHAR (200) NOT NULL,
    [DisplayName]          NVARCHAR (200) NOT NULL,
    [SourceArtifactTypeId] INT            NOT NULL,
    [TargetArtifactTypeId] INT            NOT NULL,
    [ExtendedProperty]     NVARCHAR (MAX) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[LINKING_SETTINGS]...';


GO
CREATE TABLE [dbo].[LINKING_SETTINGS] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [SettingXml] NVARCHAR (MAX) NOT NULL
);


GO
PRINT N'Creating PK_LinkingSettings...';


GO
ALTER TABLE [dbo].[LINKING_SETTINGS]
    ADD CONSTRAINT [PK_LinkingSettings] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[MAPPINGS]...';


GO
CREATE TABLE [dbo].[MAPPINGS] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [LeftValue]  NVARCHAR (MAX) NOT NULL,
    [RightValue] NVARCHAR (MAX) NOT NULL
);


GO
PRINT N'Creating PK_Mappings...';


GO
ALTER TABLE [dbo].[MAPPINGS]
    ADD CONSTRAINT [PK_Mappings] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[MIGRATION_SOURCE_CONFIGS]...';


GO
CREATE TABLE [dbo].[MIGRATION_SOURCE_CONFIGS] (
    [Id]                INT      IDENTITY (1, 1) NOT NULL,
    [CreationTime]      DATETIME NOT NULL,
    [MigrationSourceId] INT      NOT NULL,
    [GeneralSettingXml] XML      NULL,
    [SettingXml]        XML      NULL,
    [SettingXmlSchema]  XML      NULL
);


GO
PRINT N'Creating PK_MigrationSourceConfigs...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCE_CONFIGS]
    ADD CONSTRAINT [PK_MigrationSourceConfigs] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_MigrationSourceConfigs...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCE_CONFIGS]
    ADD CONSTRAINT [UK_MigrationSourceConfigs] UNIQUE NONCLUSTERED ([CreationTime] ASC, [MigrationSourceId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[MIGRATION_SOURCES]...';


GO
CREATE TABLE [dbo].[MIGRATION_SOURCES] (
    [Id]               INT              IDENTITY (1, 1) NOT NULL,
    [UniqueId]         UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]     NVARCHAR (128)   NOT NULL,
    [ServerIdentifier] NVARCHAR (128)   NOT NULL,
    [ServerUrl]        NVARCHAR (400)   NOT NULL,
    [SourceIdentifier] NVARCHAR (300)   NOT NULL,
    [ProviderId]       INT              NOT NULL,
    [NativeId]         NVARCHAR (400)   NULL
);


GO
PRINT N'Creating PK_MigrationSources...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCES]
    ADD CONSTRAINT [PK_MigrationSources] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_MigrationSourcesUniqueId...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCES]
    ADD CONSTRAINT [UK_MigrationSourcesUniqueId] UNIQUE NONCLUSTERED ([UniqueId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[Old_SessionState]...';


GO
CREATE TABLE [dbo].[Old_SessionState] (
    [SessionId] NVARCHAR (MAX) NULL,
    [Variable]  NVARCHAR (256) NULL,
    [Value]     NVARCHAR (MAX) NULL
) ON [PRIMARY];


GO
PRINT N'Creating [dbo].[PROVIDERS]...';


GO
CREATE TABLE [dbo].[PROVIDERS] (
    [Id]              INT              IDENTITY (1, 1) NOT NULL,
    [ReferenceName]   UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]    NVARCHAR (128)   NOT NULL,
    [ProviderVersion] NVARCHAR (30)    NULL
);


GO
PRINT N'Creating PK_Providers...';


GO
ALTER TABLE [dbo].[PROVIDERS]
    ADD CONSTRAINT [PK_Providers] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RELATED_ARTIFACTS_RECORDS]...';


GO
CREATE TABLE [dbo].[RELATED_ARTIFACTS_RECORDS] (
    [Id]                         BIGINT          IDENTITY (1, 1) NOT NULL,
    [MigrationSourceId]          INT             NULL,
    [ItemId]                     NVARCHAR (4000) NOT NULL,
    [Relationship]               NVARCHAR (1000) NOT NULL,
    [RelatedArtifactId]          NVARCHAR (4000) NOT NULL,
    [RelationshipExistsOnServer] BIT             NOT NULL,
    [OtherProperty]              INT             NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[RELATED_ARTIFACTS_RECORDS].[MigrationSource_ItemId]...';


GO
CREATE NONCLUSTERED INDEX [MigrationSource_ItemId]
    ON [dbo].[RELATED_ARTIFACTS_RECORDS]([MigrationSourceId] ASC)
    INCLUDE([ItemId]) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[RELATED_ARTIFACTS_RECORDS].[MigrationSource_RelatedArtifactId]...';


GO
CREATE NONCLUSTERED INDEX [MigrationSource_RelatedArtifactId]
    ON [dbo].[RELATED_ARTIFACTS_RECORDS]([MigrationSourceId] ASC)
    INCLUDE([RelatedArtifactId]) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[RUNTIME_CHANGE_ACTION]...';


GO
CREATE TABLE [dbo].[RUNTIME_CHANGE_ACTION] (
    [ChangeGroupId]         BIGINT           NOT NULL,
    [ChangeActionId]        BIGINT           IDENTITY (1, 1) NOT NULL,
    [ActionId]              UNIQUEIDENTIFIER NOT NULL,
    [SourceItem]            XML              NOT NULL,
    [FromPath]              NVARCHAR (MAX)   NULL,
    [ToPath]                NVARCHAR (MAX)   NOT NULL,
    [Recursivity]           BIT              NOT NULL,
    [ExecutionOrder]        INT              NULL,
    [IsSubstituted]         BIT              NOT NULL,
    [Label]                 NVARCHAR (MAX)   NULL,
    [MergeVersionTo]        NVARCHAR (MAX)   NULL,
    [Version]               NVARCHAR (MAX)   NULL,
    [ActionData]            XML              NULL,
    [ActionComment]         NVARCHAR (MAX)   NULL,
    [StartTime]             DATETIME         NULL,
    [FinishTime]            DATETIME         NULL,
    [AnalysisPhase]         INT              NOT NULL,
    [ItemTypeReferenceName] NVARCHAR (400)   NOT NULL,
    [Backlogged]            BIT              NOT NULL
);


GO
PRINT N'Creating PK_RT_ChangeAction...';


GO
ALTER TABLE [dbo].[RUNTIME_CHANGE_ACTION]
    ADD CONSTRAINT [PK_RT_ChangeAction] PRIMARY KEY CLUSTERED ([ChangeGroupId] ASC, [ChangeActionId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_CHANGE_ACTION].[ChangeActionId_NonCluster]...';


GO
CREATE NONCLUSTERED INDEX [ChangeActionId_NonCluster]
    ON [dbo].[RUNTIME_CHANGE_ACTION]([ChangeActionId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[RUNTIME_CHANGE_GROUPS]...';


GO
CREATE TABLE [dbo].[RUNTIME_CHANGE_GROUPS] (
    [Id]                       BIGINT           IDENTITY (1, 1) NOT NULL,
    [Name]                     NVARCHAR (MAX)   NULL,
    [ExecutionOrder]           BIGINT           NOT NULL,
    [SourceMigrationSourceId]  INT              NOT NULL,
    [Owner]                    NVARCHAR (200)   NULL,
    [Comment]                  NVARCHAR (MAX)   NULL,
    [RevisionTime]             DATETIME         NULL,
    [StartTime]                DATETIME         NULL,
    [FinishTime]               DATETIME         NULL,
    [SessionUniqueId]          UNIQUEIDENTIFIER NOT NULL,
    [SourceUniqueId]           UNIQUEIDENTIFIER NOT NULL,
    [Status]                   INT              NOT NULL,
    [SessionRunId]             INT              NOT NULL,
    [ContainsBackloggedAction] BIT              NOT NULL,
    [ReflectedChangeGroupId]   BIGINT           NULL,
    [UsePagedActions]          BIT              NULL
);


GO
PRINT N'Creating PK_RT_ChangeGroups...';


GO
ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS]
    ADD CONSTRAINT [PK_RT_ChangeGroups] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_RT_ChangeGroups...';


GO
ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS]
    ADD CONSTRAINT [UK_RT_ChangeGroups] UNIQUE NONCLUSTERED ([ExecutionOrder] ASC, [SourceMigrationSourceId] ASC, [Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_CHANGE_GROUPS].[SessionUID_SourceUID_Status]...';


GO
CREATE NONCLUSTERED INDEX [SessionUID_SourceUID_Status]
    ON [dbo].[RUNTIME_CHANGE_GROUPS]([SessionUniqueId] ASC, [SourceUniqueId] ASC, [Status] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[RUNTIME_CONFLICT_COLLECTIONS]...';


GO
CREATE TABLE [dbo].[RUNTIME_CONFLICT_COLLECTIONS] (
    [Id]      INT            IDENTITY (1, 1) NOT NULL,
    [Comment] NVARCHAR (100) NULL
);


GO
PRINT N'Creating PK_RT_ConflictCollections...';


GO
ALTER TABLE [dbo].[RUNTIME_CONFLICT_COLLECTIONS]
    ADD CONSTRAINT [PK_RT_ConflictCollections] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_CONVERSION_HISTORY]...';


GO
CREATE TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] (
    [Id]                      BIGINT         IDENTITY (1, 1) NOT NULL,
    [SessionRunId]            INT            NOT NULL,
    [SourceMigrationSourceId] INT            NOT NULL,
    [SourceChangeGroupId]     BIGINT         NULL,
    [UtcWhen]                 DATETIME       NOT NULL,
    [Comment]                 NVARCHAR (MAX) NULL,
    [ContentChanged]          BIT            NOT NULL
);


GO
PRINT N'Creating PK_ConversionHistory_Id...';


GO
ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY]
    ADD CONSTRAINT [PK_ConversionHistory_Id] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_GENERAL_PERFORMANCE_DATA]...';


GO
CREATE TABLE [dbo].[RUNTIME_GENERAL_PERFORMANCE_DATA] (
    [Id]                     INT              IDENTITY (1, 1) NOT NULL,
    [SessionGroupRunId]      INT              NOT NULL,
    [SessionUniqueId]        UNIQUEIDENTIFIER NOT NULL,
    [SourceUniqueId]         UNIQUEIDENTIFIER NOT NULL,
    [CriterionReferenceName] UNIQUEIDENTIFIER NOT NULL,
    [CriterionFriendlyName]  NVARCHAR (50)    NOT NULL,
    [PerfCounter]            BIGINT           NULL,
    [PerfStartTime]          DATETIME         NULL,
    [PerfFinishTime]         DATETIME         NULL
);


GO
PRINT N'Creating PK_RT_PerfData...';


GO
ALTER TABLE [dbo].[RUNTIME_GENERAL_PERFORMANCE_DATA]
    ADD CONSTRAINT [PK_RT_PerfData] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_RT_PerfData...';


GO
ALTER TABLE [dbo].[RUNTIME_GENERAL_PERFORMANCE_DATA]
    ADD CONSTRAINT [UK_RT_PerfData] UNIQUE NONCLUSTERED ([SessionGroupRunId] ASC, [SessionUniqueId] ASC, [SourceUniqueId] ASC, [CriterionReferenceName] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_ITEM_REVISION_PAIRS]...';


GO
CREATE TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS] (
    [LeftMigrationItemId]  BIGINT NOT NULL,
    [RightMigrationItemId] BIGINT NOT NULL,
    [ConversionHistoryId]  BIGINT NULL
);


GO
PRINT N'Creating PK_RT_ItemRevPairs...';


GO
ALTER TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS]
    ADD CONSTRAINT [PK_RT_ItemRevPairs] PRIMARY KEY CLUSTERED ([LeftMigrationItemId] ASC, [RightMigrationItemId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_ITEM_REVISION_PAIRS].[LeftMigrationItemId]...';


GO
CREATE NONCLUSTERED INDEX [LeftMigrationItemId]
    ON [dbo].[RUNTIME_ITEM_REVISION_PAIRS]([LeftMigrationItemId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[RUNTIME_ITEM_REVISION_PAIRS].[RightMigrationItemId]...';


GO
CREATE NONCLUSTERED INDEX [RightMigrationItemId]
    ON [dbo].[RUNTIME_ITEM_REVISION_PAIRS]([RightMigrationItemId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[RUNTIME_MIGRATION_ITEMS]...';


GO
CREATE TABLE [dbo].[RUNTIME_MIGRATION_ITEMS] (
    [Id]          BIGINT         IDENTITY (1, 1) NOT NULL,
    [SourceId]    INT            NOT NULL,
    [ItemId]      NVARCHAR (300) NOT NULL,
    [ItemVersion] NVARCHAR (50)  NOT NULL,
    [ItemData]    XML            NULL
);


GO
PRINT N'Creating PK_RT_MigrationItems...';


GO
ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS]
    ADD CONSTRAINT [PK_RT_MigrationItems] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_RT_MigrationItems...';


GO
ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS]
    ADD CONSTRAINT [UK_RT_MigrationItems] UNIQUE NONCLUSTERED ([SourceId] ASC, [ItemId] ASC, [ItemVersion] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_MIGRATION_ITEMS].[Item_Version_NonCluster]...';


GO
CREATE NONCLUSTERED INDEX [Item_Version_NonCluster]
    ON [dbo].[RUNTIME_MIGRATION_ITEMS]([ItemVersion] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[RUNTIME_MIGRATION_ITEMS].[ItemId_Version_NonCluster]...';


GO
CREATE NONCLUSTERED INDEX [ItemId_Version_NonCluster]
    ON [dbo].[RUNTIME_MIGRATION_ITEMS]([ItemId] ASC, [ItemVersion] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);


GO
PRINT N'Creating [dbo].[RUNTIME_ORCHESTRATION_COMMAND]...';


GO
CREATE TABLE [dbo].[RUNTIME_ORCHESTRATION_COMMAND] (
    [Id]             INT IDENTITY (1, 1) NOT NULL,
    [SessionGroupId] INT NOT NULL,
    [Command]        INT NOT NULL,
    [Status]         INT NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[RUNTIME_REGISTERED_ACTIONS]...';


GO
CREATE TABLE [dbo].[RUNTIME_REGISTERED_ACTIONS] (
    [Id]            INT              IDENTITY (1, 1) NOT NULL,
    [ReferenceName] UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]  NVARCHAR (30)    NOT NULL
);


GO
PRINT N'Creating PK_RT_RegisteredActions...';


GO
ALTER TABLE [dbo].[RUNTIME_REGISTERED_ACTIONS]
    ADD CONSTRAINT [PK_RT_RegisteredActions] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_RT_RegisteredActions...';


GO
ALTER TABLE [dbo].[RUNTIME_REGISTERED_ACTIONS]
    ADD CONSTRAINT [UK_RT_RegisteredActions] UNIQUE NONCLUSTERED ([ReferenceName] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_SESSION_GROUP_RUNS]...';


GO
CREATE TABLE [dbo].[RUNTIME_SESSION_GROUP_RUNS] (
    [Id]                   INT      IDENTITY (1, 1) NOT NULL,
    [StartTime]            DATETIME NOT NULL,
    [EndTime]              DATETIME NULL,
    [SessionGroupConfigId] INT      NOT NULL,
    [ConflictCollectionId] INT      NOT NULL
);


GO
PRINT N'Creating PK_RT_SessionGroupRuns...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSION_GROUP_RUNS]
    ADD CONSTRAINT [PK_RT_SessionGroupRuns] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_RT_SessionGroupRuns...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSION_GROUP_RUNS]
    ADD CONSTRAINT [UK_RT_SessionGroupRuns] UNIQUE NONCLUSTERED ([StartTime] ASC, [SessionGroupConfigId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_SESSION_RUNS]...';


GO
CREATE TABLE [dbo].[RUNTIME_SESSION_RUNS] (
    [Id]                   INT            IDENTITY (1, 1) NOT NULL,
    [ConfigurationId]      INT            NOT NULL,
    [LeftHighWaterMark]    NVARCHAR (128) NULL,
    [RightHighWaterMark]   NVARCHAR (128) NULL,
    [State]                INT            NULL,
    [IsPreview]            BIT            NOT NULL,
    [SessionGroupRunId]    INT            NOT NULL,
    [StartTime]            DATETIME       NULL,
    [EndTime]              DATETIME       NULL,
    [ConflictCollectionId] INT            NOT NULL
);


GO
PRINT N'Creating PK_RT_SessionRuns...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSION_RUNS]
    ADD CONSTRAINT [PK_RT_SessionRuns] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[RUNTIME_SESSIONS]...';


GO
CREATE TABLE [dbo].[RUNTIME_SESSIONS] (
    [Id]                        INT              IDENTITY (1, 1) NOT NULL,
    [SessionUniqueId]           UNIQUEIDENTIFIER NOT NULL,
    [LeftSourceId]              INT              NOT NULL,
    [RightSourceId]             INT              NOT NULL,
    [LeftHighWaterMarkLatest]   INT              NULL,
    [RightHighWaterMarkLatest]  INT              NULL,
    [LeftHighWaterMarkCurrent]  INT              NULL,
    [RightHighWaterMarkCurrent] INT              NULL,
    [SessionGroupId]            INT              NOT NULL,
    [ExecOrderInSessionGroup]   INT              NULL,
    [State]                     INT              NULL,
    [OrchestrationStatus]       INT              NULL
);


GO
PRINT N'Creating PK_RT_Sessions...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS]
    ADD CONSTRAINT [PK_RT_Sessions] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_RT_Sessions...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS]
    ADD CONSTRAINT [UK_RT_Sessions] UNIQUE NONCLUSTERED ([SessionUniqueId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[SERVER_DIFF_RESULT]...';


GO
CREATE TABLE [dbo].[SERVER_DIFF_RESULT] (
    [Id]               BIGINT           IDENTITY (1, 1) NOT NULL,
    [DiffType]         NVARCHAR (50)    NOT NULL,
    [DiffTime]         DATETIME         NOT NULL,
    [DurationOfDiff]   INT              NOT NULL,
    [SessionUniqueId]  UNIQUEIDENTIFIER NOT NULL,
    [AllContentsMatch] BIT              NOT NULL,
    [Options]          NVARCHAR (MAX)   NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[SERVER_DIFF_RESULT_DETAIL]...';


GO
CREATE TABLE [dbo].[SERVER_DIFF_RESULT_DETAIL] (
    [Id]                 BIGINT         IDENTITY (1, 1) NOT NULL,
    [ServerDiffResultId] BIGINT         NOT NULL,
    [DiffDescription]    NVARCHAR (MAX) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);


GO
PRINT N'Creating [dbo].[SESSION_CONFIGURATIONS]...';


GO
CREATE TABLE [dbo].[SESSION_CONFIGURATIONS] (
    [Id]                   INT              IDENTITY (1, 1) NOT NULL,
    [SessionUniqueId]      UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]         NVARCHAR (128)   NOT NULL,
    [SessionGroupConfigId] INT              NOT NULL,
    [CreationTime]         DATETIME         NOT NULL,
    [Creator]              NVARCHAR (50)    NULL,
    [DeprecationTime]      DATETIME         NULL,
    [LeftSourceConfigId]   INT              NOT NULL,
    [RightSourceConfigId]  INT              NOT NULL,
    [Type]                 INT              NOT NULL,
    [SettingXml]           XML              NULL,
    [SettingXmlSchema]     XML              NULL
);


GO
PRINT N'Creating PK_SessionConfigurations...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS]
    ADD CONSTRAINT [PK_SessionConfigurations] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_SessionConfigurations...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS]
    ADD CONSTRAINT [UK_SessionConfigurations] UNIQUE NONCLUSTERED ([SessionUniqueId] ASC, [SessionGroupConfigId] ASC, [CreationTime] ASC, [LeftSourceConfigId] ASC, [RightSourceConfigId] ASC, [Type] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[SESSION_GROUP_CONFIG_CREATION_WORKTBL]...';


GO
CREATE TABLE [dbo].[SESSION_GROUP_CONFIG_CREATION_WORKTBL] (
    [PreviousSessionGroupConfigId] INT NOT NULL,
    [NewSessionGroupConfigId]      INT NOT NULL
);


GO
PRINT N'Creating [dbo].[SESSION_GROUP_CONFIGS]...';


GO
CREATE TABLE [dbo].[SESSION_GROUP_CONFIGS] (
    [Id]                         INT              IDENTITY (1, 1) NOT NULL,
    [CreationTime]               DATETIME         NOT NULL,
    [Creator]                    NVARCHAR (50)    NULL,
    [DeprecationTime]            DATETIME         NULL,
    [Status]                     INT              NOT NULL,
    [SessionGroupId]             INT              NOT NULL,
    [LinkingSettingId]           INT              NULL,
    [FriendlyName]               NVARCHAR (300)   NULL,
    [UniqueId]                   UNIQUEIDENTIFIER NOT NULL,
    [WorkFlowType]               INT              NOT NULL,
    [UserIdentityMappingsConfig] XML              NULL,
    [ErrorManagementConfig]      XML              NULL,
    [AddinsConfig]               XML              NULL,
    [Settings]                   XML              NULL
);


GO
PRINT N'Creating PK_SessionGroupConfigs...';


GO
ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS]
    ADD CONSTRAINT [PK_SessionGroupConfigs] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_SessionGroupConfigs1...';


GO
ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS]
    ADD CONSTRAINT [UK_SessionGroupConfigs1] UNIQUE NONCLUSTERED ([UniqueId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[SESSION_GROUPS]...';


GO
CREATE TABLE [dbo].[SESSION_GROUPS] (
    [Id]                  INT              IDENTITY (1, 1) NOT NULL,
    [GroupUniqueId]       UNIQUEIDENTIFIER NOT NULL,
    [FriendlyName]        NVARCHAR (128)   NOT NULL,
    [State]               INT              NULL,
    [OrchestrationStatus] INT              NULL
);


GO
PRINT N'Creating PK_SessionGroups...';


GO
ALTER TABLE [dbo].[SESSION_GROUPS]
    ADD CONSTRAINT [PK_SessionGroups] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_SessionGroups...';


GO
ALTER TABLE [dbo].[SESSION_GROUPS]
    ADD CONSTRAINT [UK_SessionGroups] UNIQUE NONCLUSTERED ([GroupUniqueId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[STORED_CREDENTIALS]...';


GO
CREATE TABLE [dbo].[STORED_CREDENTIALS] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [CredentialString]  NVARCHAR (300) NOT NULL,
    [MigrationSourceId] INT            NOT NULL
);


GO
PRINT N'Creating PK_StoredCredentials...';


GO
ALTER TABLE [dbo].[STORED_CREDENTIALS]
    ADD CONSTRAINT [PK_StoredCredentials] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_StoredCredentials...';


GO
ALTER TABLE [dbo].[STORED_CREDENTIALS]
    ADD CONSTRAINT [UK_StoredCredentials] UNIQUE NONCLUSTERED ([MigrationSourceId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[SYNC_POINT]...';


GO
CREATE TABLE [dbo].[SYNC_POINT] (
    [Id]                            BIGINT           IDENTITY (1, 1) NOT NULL,
    [SessionUniqueId]               UNIQUEIDENTIFIER NOT NULL,
    [SourceUniqueId]                UNIQUEIDENTIFIER NOT NULL,
    [SourceHighWaterMarkName]       NVARCHAR (50)    NOT NULL,
    [SourceHighWaterMarkValue]      NVARCHAR (50)    NULL,
    [LastMigratedTargetItemId]      NVARCHAR (300)   NOT NULL,
    [LastMigratedTargetItemVersion] NVARCHAR (50)    NOT NULL,
    [LastChangeGroupId]             BIGINT           NULL
);


GO
PRINT N'Creating PK_SyncPoint...';


GO
ALTER TABLE [dbo].[SYNC_POINT]
    ADD CONSTRAINT [PK_SyncPoint] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[VALUE_MAP_COLLECTION]...';


GO
CREATE TABLE [dbo].[VALUE_MAP_COLLECTION] (
    [FieldMapCollectionId] INT NULL,
    [MappingId]            INT NOT NULL
);


GO
PRINT N'Creating PK_ValueMapCollection...';


GO
ALTER TABLE [dbo].[VALUE_MAP_COLLECTION]
    ADD CONSTRAINT [PK_ValueMapCollection] PRIMARY KEY CLUSTERED ([MappingId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_ValueMapCollection...';


GO
ALTER TABLE [dbo].[VALUE_MAP_COLLECTION]
    ADD CONSTRAINT [UK_ValueMapCollection] UNIQUE NONCLUSTERED ([MappingId] ASC, [FieldMapCollectionId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating [dbo].[WI_TYPE_MAP_COLLECTION]...';


GO
CREATE TABLE [dbo].[WI_TYPE_MAP_COLLECTION] (
    [Id]        INT IDENTITY (1, 1) NOT NULL,
    [MappingId] INT NOT NULL
);


GO
PRINT N'Creating PK_WITypeMapCollection...';


GO
ALTER TABLE [dbo].[WI_TYPE_MAP_COLLECTION]
    ADD CONSTRAINT [PK_WITypeMapCollection] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating UK_WITypeMapCollection...';


GO
ALTER TABLE [dbo].[WI_TYPE_MAP_COLLECTION]
    ADD CONSTRAINT [UK_WITypeMapCollection] UNIQUE NONCLUSTERED ([MappingId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating FK_ConfigCheckoutRecords...';


GO
ALTER TABLE [dbo].[CONFIG_CHECKOUT_RECORDS] WITH NOCHECK
    ADD CONSTRAINT [FK_ConfigCheckoutRecords] FOREIGN KEY ([SessionGroupConfigId]) REFERENCES [dbo].[SESSION_GROUP_CONFIGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_to_provider...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICT_TYPES] WITH NOCHECK
    ADD CONSTRAINT [FK_to_provider] FOREIGN KEY ([ProviderId]) REFERENCES [dbo].[PROVIDERS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Conflicts_to_ChangeAction...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH NOCHECK
    ADD CONSTRAINT [FK_Conflicts_to_ChangeAction] FOREIGN KEY ([ChangeGroupId], [ConflictedChangeActionId]) REFERENCES [dbo].[RUNTIME_CHANGE_ACTION] ([ChangeGroupId], [ChangeActionId]) ON DELETE SET NULL ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Conflicts_to_ConflictCollection...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH NOCHECK
    ADD CONSTRAINT [FK_Conflicts_to_ConflictCollection] FOREIGN KEY ([ConflictListId]) REFERENCES [dbo].[RUNTIME_CONFLICT_COLLECTIONS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Conflicts_to_ConflictType...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH NOCHECK
    ADD CONSTRAINT [FK_Conflicts_to_ConflictType] FOREIGN KEY ([ConflictTypeId]) REFERENCES [dbo].[CONFLICT_CONFLICT_TYPES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Conflicts_to_LinkChangeAction...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH NOCHECK
    ADD CONSTRAINT [FK_Conflicts_to_LinkChangeAction] FOREIGN KEY ([ConflictedLinkChangeActionId]) REFERENCES [dbo].[LINK_LINK_CHANGE_ACTIONS] ([Id]) ON DELETE SET NULL ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Conflicts_to_LinkChangeGroup...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH NOCHECK
    ADD CONSTRAINT [FK_Conflicts_to_LinkChangeGroup] FOREIGN KEY ([ConflictedLinkChangeGroupId]) REFERENCES [dbo].[LINK_LINK_CHANGE_GROUPS] ([Id]) ON DELETE SET NULL ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Conflicts_to_MigrationSource...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH NOCHECK
    ADD CONSTRAINT [FK_Conflicts_to_MigrationSource] FOREIGN KEY ([SourceMigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Conflicts_to_ResolveRule...';


GO
ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH NOCHECK
    ADD CONSTRAINT [FK_Conflicts_to_ResolveRule] FOREIGN KEY ([ResolvedByRuleId]) REFERENCES [dbo].[CONFLICT_RESOLUTION_RULES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ContentResv1...';


GO
ALTER TABLE [dbo].[CONFLICT_CONTENT_RESV] WITH NOCHECK
    ADD CONSTRAINT [FK_ContentResv1] FOREIGN KEY ([ConflictId]) REFERENCES [dbo].[CONFLICT_CONFLICTS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ContentResv2...';


GO
ALTER TABLE [dbo].[CONFLICT_CONTENT_RESV] WITH NOCHECK
    ADD CONSTRAINT [FK_ContentResv2] FOREIGN KEY ([ItemId]) REFERENCES [dbo].[RUNTIME_MIGRATION_ITEMS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_action_to_provider...';


GO
ALTER TABLE [dbo].[CONFLICT_RESOLUTION_ACTIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_action_to_provider] FOREIGN KEY ([ProviderId]) REFERENCES [dbo].[PROVIDERS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ResolutionRule_to_ResolutionAction...';


GO
ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES] WITH NOCHECK
    ADD CONSTRAINT [FK_ResolutionRule_to_ResolutionAction] FOREIGN KEY ([ResolutionActionId]) REFERENCES [dbo].[CONFLICT_RESOLUTION_ACTIONS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ResolutionRules1...';


GO
ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES] WITH NOCHECK
    ADD CONSTRAINT [FK_ResolutionRules1] FOREIGN KEY ([ScopeId]) REFERENCES [dbo].[CONFLICT_RULE_SCOPES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ResolutionRules2...';


GO
ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES] WITH NOCHECK
    ADD CONSTRAINT [FK_ResolutionRules2] FOREIGN KEY ([ConflictTypeId]) REFERENCES [dbo].[CONFLICT_CONFLICT_TYPES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_EventSink...';


GO
ALTER TABLE [dbo].[EVENT_SINK] WITH NOCHECK
    ADD CONSTRAINT [FK_EventSink] FOREIGN KEY ([ProviderId]) REFERENCES [dbo].[PROVIDERS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_EventSinkJunc1...';


GO
ALTER TABLE [dbo].[EVENT_SINK_JUNC] WITH NOCHECK
    ADD CONSTRAINT [FK_EventSinkJunc1] FOREIGN KEY ([EventSinkId]) REFERENCES [dbo].[EVENT_SINK] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_EventSinkJunc2...';


GO
ALTER TABLE [dbo].[EVENT_SINK_JUNC] WITH NOCHECK
    ADD CONSTRAINT [FK_EventSinkJunc2] FOREIGN KEY ([SessionConfigId]) REFERENCES [dbo].[SESSION_CONFIGURATIONS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_FieldExclCollection...';


GO
ALTER TABLE [dbo].[FIELD_EXCLUSION_COLLECTION] WITH NOCHECK
    ADD CONSTRAINT [FK_FieldExclCollection] FOREIGN KEY ([WITypeMapId]) REFERENCES [dbo].[WI_TYPE_MAP_COLLECTION] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_FieldMapCollection1...';


GO
ALTER TABLE [dbo].[FIELD_MAP_COLLECTION] WITH NOCHECK
    ADD CONSTRAINT [FK_FieldMapCollection1] FOREIGN KEY ([WITypeMapId]) REFERENCES [dbo].[WI_TYPE_MAP_COLLECTION] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_FieldMapCollection2...';


GO
ALTER TABLE [dbo].[FIELD_MAP_COLLECTION] WITH NOCHECK
    ADD CONSTRAINT [FK_FieldMapCollection2] FOREIGN KEY ([ValueMappingId]) REFERENCES [dbo].[MAPPINGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_FilterItemPair_to_SessionConfig...';


GO
ALTER TABLE [dbo].[FILTER_ITEM_PAIR] WITH NOCHECK
    ADD CONSTRAINT [FK_FilterItemPair_to_SessionConfig] FOREIGN KEY ([SessionConfigId]) REFERENCES [dbo].[SESSION_CONFIGURATIONS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating LATENCY_POLL.FK_MigrationSource.fkey...';


GO
ALTER TABLE [dbo].[LATENCY_POLL] WITH NOCHECK
    ADD CONSTRAINT [LATENCY_POLL.FK_MigrationSource.fkey] FOREIGN KEY ([MigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ArtifactLink_to_LinkType...';


GO
ALTER TABLE [dbo].[LINK_ARTIFACT_LINK] WITH NOCHECK
    ADD CONSTRAINT [FK_ArtifactLink_to_LinkType] FOREIGN KEY ([LinkTypeId]) REFERENCES [dbo].[LINK_LINK_TYPE] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_LinkChangeAction_to_ArtifactLink...';


GO
ALTER TABLE [dbo].[LINK_LINK_CHANGE_ACTIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_LinkChangeAction_to_ArtifactLink] FOREIGN KEY ([ArtifactLinkId]) REFERENCES [dbo].[LINK_ARTIFACT_LINK] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_LinkChangeAction_to_LinkChangeGroup...';


GO
ALTER TABLE [dbo].[LINK_LINK_CHANGE_ACTIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_LinkChangeAction_to_LinkChangeGroup] FOREIGN KEY ([LinkChangeGroupId]) REFERENCES [dbo].[LINK_LINK_CHANGE_GROUPS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_LinkType_to_ArtifactTypeSource...';


GO
ALTER TABLE [dbo].[LINK_LINK_TYPE] WITH NOCHECK
    ADD CONSTRAINT [FK_LinkType_to_ArtifactTypeSource] FOREIGN KEY ([SourceArtifactTypeId]) REFERENCES [dbo].[LINK_ARTIFACT_TYPE] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_LinkType_to_ArtifactTypeTarget...';


GO
ALTER TABLE [dbo].[LINK_LINK_TYPE] WITH NOCHECK
    ADD CONSTRAINT [FK_LinkType_to_ArtifactTypeTarget] FOREIGN KEY ([TargetArtifactTypeId]) REFERENCES [dbo].[LINK_ARTIFACT_TYPE] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_MigrationSourceConfigs...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCE_CONFIGS] WITH NOCHECK
    ADD CONSTRAINT [FK_MigrationSourceConfigs] FOREIGN KEY ([MigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_MigrationSources1...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCES] WITH NOCHECK
    ADD CONSTRAINT [FK_MigrationSources1] FOREIGN KEY ([ProviderId]) REFERENCES [dbo].[PROVIDERS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RelatedRecord_to_MigrationSource...';


GO
ALTER TABLE [dbo].[RELATED_ARTIFACTS_RECORDS] WITH NOCHECK
    ADD CONSTRAINT [FK_RelatedRecord_to_MigrationSource] FOREIGN KEY ([MigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_ChangeActions...';


GO
ALTER TABLE [dbo].[RUNTIME_CHANGE_ACTION] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ChangeActions] FOREIGN KEY ([ChangeGroupId]) REFERENCES [dbo].[RUNTIME_CHANGE_GROUPS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_ChangeGroups_to_SessionRun...';


GO
ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ChangeGroups_to_SessionRun] FOREIGN KEY ([SessionRunId]) REFERENCES [dbo].[RUNTIME_SESSION_RUNS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_ChangeGroups1...';


GO
ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ChangeGroups1] FOREIGN KEY ([SourceMigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ConvHistory_to_ChangeGroup...';


GO
ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] WITH NOCHECK
    ADD CONSTRAINT [FK_ConvHistory_to_ChangeGroup] FOREIGN KEY ([SourceChangeGroupId]) REFERENCES [dbo].[RUNTIME_CHANGE_GROUPS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ConvHistory_to_MigrationSource...';


GO
ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] WITH NOCHECK
    ADD CONSTRAINT [FK_ConvHistory_to_MigrationSource] FOREIGN KEY ([SourceMigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ConvHistory_to_SessionRun...';


GO
ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] WITH NOCHECK
    ADD CONSTRAINT [FK_ConvHistory_to_SessionRun] FOREIGN KEY ([SessionRunId]) REFERENCES [dbo].[RUNTIME_SESSION_RUNS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_PerfData_To_SessionGroupRun...';


GO
ALTER TABLE [dbo].[RUNTIME_GENERAL_PERFORMANCE_DATA] WITH NOCHECK
    ADD CONSTRAINT [FK_PerfData_To_SessionGroupRun] FOREIGN KEY ([SessionGroupRunId]) REFERENCES [dbo].[RUNTIME_SESSION_GROUP_RUNS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_ItemRevPairs...';


GO
ALTER TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ItemRevPairs] FOREIGN KEY ([ConversionHistoryId]) REFERENCES [dbo].[RUNTIME_CONVERSION_HISTORY] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_ItemRevPairs1...';


GO
ALTER TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ItemRevPairs1] FOREIGN KEY ([LeftMigrationItemId]) REFERENCES [dbo].[RUNTIME_MIGRATION_ITEMS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_ItemRevPairs2...';


GO
ALTER TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ItemRevPairs2] FOREIGN KEY ([RightMigrationItemId]) REFERENCES [dbo].[RUNTIME_MIGRATION_ITEMS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_MigrationItems...';


GO
ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_MigrationItems] FOREIGN KEY ([SourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_OrchCmd_to_SessionGroup...';


GO
ALTER TABLE [dbo].[RUNTIME_ORCHESTRATION_COMMAND] WITH NOCHECK
    ADD CONSTRAINT [FK_OrchCmd_to_SessionGroup] FOREIGN KEY ([SessionGroupId]) REFERENCES [dbo].[SESSION_GROUPS] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_SessionGroupRuns1...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSION_GROUP_RUNS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_SessionGroupRuns1] FOREIGN KEY ([SessionGroupConfigId]) REFERENCES [dbo].[SESSION_GROUP_CONFIGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_SessionRuns1...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSION_RUNS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_SessionRuns1] FOREIGN KEY ([ConfigurationId]) REFERENCES [dbo].[SESSION_CONFIGURATIONS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_SessionRuns2...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSION_RUNS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_SessionRuns2] FOREIGN KEY ([SessionGroupRunId]) REFERENCES [dbo].[RUNTIME_SESSION_GROUP_RUNS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_Sessions1...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_Sessions1] FOREIGN KEY ([LeftSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_Sessions2...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_Sessions2] FOREIGN KEY ([RightSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_RT_Sessions3...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_Sessions3] FOREIGN KEY ([SessionGroupId]) REFERENCES [dbo].[SESSION_GROUPS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_SERVER_DIFF_RESULT.FK_SERVER_DIFF_RESULT.fkey...';


GO
ALTER TABLE [dbo].[SERVER_DIFF_RESULT_DETAIL] WITH NOCHECK
    ADD CONSTRAINT [FK_SERVER_DIFF_RESULT.FK_SERVER_DIFF_RESULT.fkey] FOREIGN KEY ([ServerDiffResultId]) REFERENCES [dbo].[SERVER_DIFF_RESULT] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_SessionConfiguration5...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_SessionConfiguration5] FOREIGN KEY ([SessionGroupConfigId]) REFERENCES [dbo].[SESSION_GROUP_CONFIGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_SessionConfigurations2...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_SessionConfigurations2] FOREIGN KEY ([LeftSourceConfigId]) REFERENCES [dbo].[MIGRATION_SOURCE_CONFIGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_SessionConfigurations3...';


GO
ALTER TABLE [dbo].[SESSION_CONFIGURATIONS] WITH NOCHECK
    ADD CONSTRAINT [FK_SessionConfigurations3] FOREIGN KEY ([RightSourceConfigId]) REFERENCES [dbo].[MIGRATION_SOURCE_CONFIGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_SessionGroupConfigs1...';


GO
ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS] WITH NOCHECK
    ADD CONSTRAINT [FK_SessionGroupConfigs1] FOREIGN KEY ([SessionGroupId]) REFERENCES [dbo].[SESSION_GROUPS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_SessionGroupConfigs3...';


GO
ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS] WITH NOCHECK
    ADD CONSTRAINT [FK_SessionGroupConfigs3] FOREIGN KEY ([LinkingSettingId]) REFERENCES [dbo].[LINKING_SETTINGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_StoredCredentials...';


GO
ALTER TABLE [dbo].[STORED_CREDENTIALS] WITH NOCHECK
    ADD CONSTRAINT [FK_StoredCredentials] FOREIGN KEY ([MigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ValueMapCollection1...';


GO
ALTER TABLE [dbo].[VALUE_MAP_COLLECTION] WITH NOCHECK
    ADD CONSTRAINT [FK_ValueMapCollection1] FOREIGN KEY ([MappingId]) REFERENCES [dbo].[MAPPINGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_ValueMapCollection2...';


GO
ALTER TABLE [dbo].[VALUE_MAP_COLLECTION] WITH NOCHECK
    ADD CONSTRAINT [FK_ValueMapCollection2] FOREIGN KEY ([FieldMapCollectionId]) REFERENCES [dbo].[FIELD_MAP_COLLECTION] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_WITypeMapCollection...';


GO
ALTER TABLE [dbo].[WI_TYPE_MAP_COLLECTION] WITH NOCHECK
    ADD CONSTRAINT [FK_WITypeMapCollection] FOREIGN KEY ([MappingId]) REFERENCES [dbo].[MAPPINGS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating [dbo].[BatchUpdateLinkChangeGroupStatus]...';


GO
CREATE PROCEDURE [dbo].[BatchUpdateLinkChangeGroupStatus]
	@SessionGroupId uniqueidentifier, 
	@SessionId uniqueidentifier,
	@SourceId uniqueidentifier,
	@ContainsConflictedAction bit,
	@CurrentStatus int,
	@NewStatus int
AS
	UPDATE [dbo].[LINK_LINK_CHANGE_GROUPS]
	set Status = @NewStatus
	where Status = @CurrentStatus
	  and SessionGroupUniqueId = @SessionGroupId
	  and SessionUniqueId = @SessionId
	  and SourceId = @SourceId
	  and ContainsConflictedAction = @ContainsConflictedAction;
	
	select *
	from LINK_LINK_CHANGE_GROUPS
	where Status = @NewStatus
	  and SessionGroupUniqueId = @SessionGroupId
	  and SessionUniqueId = @SessionId
	  and SourceId = @SourceId
	  and ContainsConflictedAction = @ContainsConflictedAction;
GO
PRINT N'Creating [dbo].[ConfigCheckoutRecordInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordInsert
--GO

CREATE PROCEDURE dbo.ConfigCheckoutRecordInsert
(
	@SessionGroupConfigId int,
	@CheckOutToken uniqueidentifier
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[CONFIG_CHECKOUT_RECORDS] ([SessionGroupConfigId], [CheckOutToken]) VALUES (@SessionGroupConfigId, @CheckOutToken);
	
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO
PRINT N'Creating [dbo].[ConfigCheckoutRecordUpdateLock]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordUpdateLock' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordUpdateLock
--GO

CREATE PROCEDURE dbo.ConfigCheckoutRecordUpdateLock
(
	@CheckOutToken uniqueidentifier,
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT OFF;
UPDATE       CONFIG_CHECKOUT_RECORDS
SET                CheckOutToken = @CheckOutToken
WHERE        (SessionGroupConfigId = @SessionGroupConfigId);
	 
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO
PRINT N'Creating [dbo].[ConfigCheckoutRecordUpdateUnlock]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordUpdateUnlock' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordUpdateUnlock
--GO

CREATE PROCEDURE dbo.ConfigCheckoutRecordUpdateUnlock
(
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT OFF;
UPDATE       CONFIG_CHECKOUT_RECORDS
SET                CheckOutToken = NULL
WHERE        (SessionGroupConfigId = @SessionGroupConfigId );
	 
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO
PRINT N'Creating [dbo].[EventSinkDelete]...';


GO
CREATE PROCEDURE dbo.EventSinkDelete
(
	@Original_EventSinkId int,
	@Original_SessionConfigId int
)
AS
	SET NOCOUNT OFF;
DELETE FROM EVENT_SINK_JUNC
WHERE        (EventSinkId = @Original_EventSinkId) AND (SessionConfigId = @Original_SessionConfigId)
GO
PRINT N'Creating [dbo].[EventSinkInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkInsert
--GO

CREATE PROCEDURE dbo.EventSinkInsert
(
	@FriendlyName nvarchar(128),
	@ProviderId int,
	@CreationTime datetime,
	@SettingXml xml,
	@SettingXmlSchema xml
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[EVENT_SINK] ([FriendlyName], [ProviderId], [CreationTime], [SettingXml], [SettingXmlSchema]) VALUES (@FriendlyName, @ProviderId, @CreationTime, @SettingXml, @SettingXmlSchema);
	
SELECT Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema FROM EVENT_SINK WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Creating [dbo].[EventSinkJuncInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkJuncInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkJuncInsert
--GO

CREATE PROCEDURE dbo.EventSinkJuncInsert
(
	@EventSinkId int,
	@SessionConfigId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[EVENT_SINK_JUNC] ([EventSinkId], [SessionConfigId]) VALUES (@EventSinkId, @SessionConfigId);
	
SELECT EventSinkId, SessionConfigId FROM EVENT_SINK_JUNC WHERE (EventSinkId = @EventSinkId) AND (SessionConfigId = @SessionConfigId)
GO
PRINT N'Creating [dbo].[EventSinkJuncSelectBySessionConfigId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkJuncSelectBySessionConfigId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkJuncSelectBySessionConfigId
--GO

CREATE PROCEDURE dbo.EventSinkJuncSelectBySessionConfigId
(
	@SessionConfigId int
)
AS
	SET NOCOUNT ON;
SELECT        EventSinkId, SessionConfigId
FROM            EVENT_SINK_JUNC
WHERE        (SessionConfigId = @SessionConfigId)
GO
PRINT N'Creating [dbo].[EventSinkSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkSelectById
--GO

CREATE PROCEDURE dbo.EventSinkSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema
FROM            EVENT_SINK
WHERE        (Id = @Id)
GO
PRINT N'Creating [dbo].[EventSinkSelectByProviderAndCreationTime]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkSelectByProviderAndCreationTime' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkSelectByProviderAndCreationTime
--GO

CREATE PROCEDURE dbo.EventSinkSelectByProviderAndCreationTime
(
	@ProviderId int,
	@CreationTime datetime
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema
FROM            EVENT_SINK
WHERE        (ProviderId = @ProviderId) AND (CreationTime = @CreationTime)
GO
PRINT N'Creating [dbo].[EventSinkUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkUpdate
--GO

CREATE PROCEDURE dbo.EventSinkUpdate
(
	@FriendlyName nvarchar(128),
	@SettingXml xml,
	@SettingXmlSchema xml,
	@Original_ProviderId int,
	@Original_CreationTime datetime
)
AS
	SET NOCOUNT OFF;
UPDATE       EVENT_SINK
SET                FriendlyName = @FriendlyName, SettingXml = @SettingXml, SettingXmlSchema = @SettingXmlSchema
WHERE        (ProviderId = @Original_ProviderId) AND (CreationTime = @Original_CreationTime);
	 
SELECT Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema FROM EVENT_SINK WHERE (ProviderId = @Original_ProviderId) AND (CreationTime = @Original_CreationTime)
GO
PRINT N'Creating [dbo].[FindCreateArtifactLink]...';


GO
CREATE PROCEDURE [dbo].[FindCreateArtifactLink]
	@SourceArtifactUri nvarchar(400),
	@TargetArtifactUri nvarchar(400),
	@LinkTypeId int,
	@Comment nvarchar(max),
	@SourceArtifactId nvarchar(400),
	@CreateOnMissing bit,
	@Id int output
AS
	SELECT @Id = Id
	from LINK_ARTIFACT_LINK
	where SourceArtifactUri = @SourceArtifactUri
	and TargetArtifactUri = @TargetArtifactUri
	and LinkTypeId = @LinkTypeId
	and SourceArtifactId = @SourceArtifactId
	
	if @Id is null AND @CreateOnMissing = 1
	begin
		insert into LINK_ARTIFACT_LINK 
			(SourceArtifactUri, TargetArtifactUri, LinkTypeId, Comment, SourceArtifactId)
			values (@SourceArtifactUri, @TargetArtifactUri, @LinkTypeId, @Comment, @SourceArtifactId)
		select @Id = @@identity;
	end
	else
	begin
		update LINK_ARTIFACT_LINK
		set Comment = @Comment
		where Id = @Id
	end
	
	if @Id is null
	begin
		select @Id = -1;
	end
RETURN 0;
GO
PRINT N'Creating [dbo].[FindCreateArtifactType]...';


GO
CREATE PROCEDURE [dbo].[FindCreateArtifactType]
	@ReferenceName nvarchar(200),
	@DisplayName nvarchar(200),
	@ArtifactContentType nvarchar(400),
	@Id int output
AS
	select @Id = Id
	from LINK_ARTIFACT_TYPE
	where ReferenceName = @ReferenceName
	and DisplayName = @DisplayName
	and ArtifactContentType = @ArtifactContentType
	
	if @Id is null
	begin
		insert into LINK_ARTIFACT_TYPE (ReferenceName, DisplayName, ArtifactContentType)
			values (@ReferenceName, @DisplayName, @ArtifactContentType);
		select @Id = @@identity;
	end
RETURN 0;
GO
PRINT N'Creating [dbo].[FindCreateLinkType]...';


GO
CREATE PROCEDURE [dbo].[FindCreateLinkType]
	@ReferenceName nvarchar(200),
	@DisplayName nvarchar(200),
	@SourceArtifactTypeId int,
	@TargetArtifactTypeId int,
	@ExtendedProperty nvarchar(max),
	@Id int output
AS
	SELECT @Id = Id
	from LINK_LINK_TYPE
	where ReferenceName = @ReferenceName
	and DisplayName = @DisplayName
	and SourceArtifactTypeId = @SourceArtifactTypeId
	and TargetArtifactTypeId = @TargetArtifactTypeId;
	
	if @Id is null
	begin
		insert into LINK_LINK_TYPE (ReferenceName, DisplayName, SourceArtifactTypeId, TargetArtifactTypeId, ExtendedProperty)
			values (@ReferenceName, @DisplayName, @SourceArtifactTypeId, @TargetArtifactTypeId, @ExtendedProperty);
		select @Id = @@identity;
	end
RETURN 0;
GO
PRINT N'Creating [dbo].[FindCreateMigrationItem]...';


GO
CREATE PROCEDURE [dbo].[FindCreateMigrationItem]
	@SourceId int,
	@ItemId NVARCHAR(50), 
	@ItemVersion NVARCHAR(50), 
	@Id bigint output
AS
	select @Id = Id
	from RUNTIME_MIGRATION_ITEMS
	where SourceId = @SourceId
	and ItemId = @ItemId
	and ItemVersion = @ItemVersion
	
	if @Id is null
	begin
		insert into RUNTIME_MIGRATION_ITEMS (SourceId, ItemId, ItemVersion)
			values (@SourceId, @ItemId, @ItemVersion);
		select @Id = @@identity;
	end
RETURN 0;
GO
PRINT N'Creating [dbo].[FindLinkChangeActionInDelta]...';


GO
CREATE PROCEDURE [dbo].[FindLinkChangeActionInDelta]
	@SessionGroupUniqueId uniqueidentifier,
	@SessionUniqueId uniqueidentifier,
	@SourceID uniqueidentifier,
	@ActionId uniqueidentifier,
	@SourceArtifactId nvarchar(400),
	@SourceArtifactUri nvarchar(400),
	@TargetArtifactUri nvarchar(400),
	@Comment nvarchar(max),
	@LinkTypeReferenceName nvarchar(200),
	@LinkTypeDisplayName nvarchar(200),
	@ExtendedLinkProperty nvarchar(max),
	@SourceArtifactTypeReferenceName nvarchar(200),
	@SourceArtifactTypeDisplayName nvarchar(200),
	@SourceArtifactContentType nvarchar(400),
	@TargetArtifactTypeReferenceName nvarchar(200),
	@TargetArtifactTypeDisplayName nvarchar(200),
	@TargetArtifactContentType nvarchar(400)
AS
	declare @LinkChangeActionId int;
    declare @SourceArtifactTypeId int;
    declare @TargetArtifactTypeId int;
    declare @LinkTypeId int;
    declare @ArtifactLinkId int;
    
    execute FindCreateArtifactType 
		@SourceArtifactTypeReferenceName, 
		@SourceArtifactTypeDisplayName, 
		@SourceArtifactContentType,
		@SourceArtifactTypeId output;
	
	execute FindCreateArtifactType
		@TargetArtifactTypeReferenceName,
		@TargetArtifactTypeDisplayName,
		@TargetArtifactContentType,
		@TargetArtifactTypeId output;
	
	execute FindCreateLinkType
		@LinkTypeReferenceName,
		@LinkTypeDisplayName,
		@SourceArtifactTypeId,
		@TargetArtifactTypeId,
		@ExtendedLinkProperty,
		@LinkTypeId output;
		
	execute FindCreateArtifactLink
		@SourceArtifactUri,
		@TargetArtifactUri,
		@LinkTypeId,
		@Comment,
		@SourceArtifactId,
		0,
		@ArtifactLinkId output;
	
	SELECT *
	FROM LINK_LINK_CHANGE_ACTIONS
	where SessionGroupUniqueId = @SessionGroupUniqueId
	and SessionUniqueId = @SessionUniqueId
	and SourceId = @SourceID
	and ActionId = @ActionId
	and ArtifactLinkId = @ArtifactLinkId
	and Status = 10
RETURN 0;
GO
PRINT N'Creating [dbo].[MigrationSourceConfigInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigInsert
--GO

CREATE PROCEDURE dbo.MigrationSourceConfigInsert
(
	@CreationTime datetime,
	@MigrationSourceId int,
	@SettingXml xml,
	@SettingXmlSchema xml
)
AS
	SET NOCOUNT OFF;
INSERT INTO MIGRATION_SOURCE_CONFIGS
                         (CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema)
VALUES        (@CreationTime,@MigrationSourceId,@SettingXml,@SettingXmlSchema);
	 
SELECT Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema FROM MIGRATION_SOURCE_CONFIGS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Creating [dbo].[MigrationSourceConfigSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigSelectById
--GO

CREATE PROCEDURE dbo.MigrationSourceConfigSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (Id = @Id)
GO
PRINT N'Creating [dbo].[MigrationSourceConfigSelectByMigrationSourceId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigSelectByMigrationSourceId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigSelectByMigrationSourceId
--GO

CREATE PROCEDURE dbo.MigrationSourceConfigSelectByMigrationSourceId
(
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (MigrationSourceId = @MigrationSourceId)
GO
PRINT N'Creating [dbo].[MigrationSourceConfigSelectBySourceIdAndCreationTime]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigsSelectBySourceIdAndCreationTime' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigsSelectBySourceIdAndCreationTime
--GO

CREATE PROCEDURE dbo.MigrationSourceConfigSelectBySourceIdAndCreationTime
(
	@CreationTime datetime,
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (CreationTime = @CreationTime) AND (MigrationSourceId = @MigrationSourceId)
GO
PRINT N'Creating [dbo].[MigrationSourceInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceInsert
--GO

CREATE PROCEDURE dbo.MigrationSourceInsert
(
	@FriendlyName nvarchar(128),
	@ServerIdentifier nvarchar(128),
	@ServerUrl nvarchar(400),
	@SourceIdentifier nvarchar(400),
	@ProviderId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO MIGRATION_SOURCES
                         (FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId)
VALUES        (@FriendlyName,@ServerIdentifier,@ServerUrl,@SourceIdentifier,@ProviderId);
	 
SELECT Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId FROM MIGRATION_SOURCES WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Creating [dbo].[MigrationSourceSelectByConfigInfo]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceSelectByConfigInfo' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceSelectByConfigInfo
--GO

CREATE PROCEDURE dbo.MigrationSourceSelectByConfigInfo
(
	@ServerIdentifier nvarchar(128),
	@SourceIdentifier nvarchar(400),
	@ProviderId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId
FROM            MIGRATION_SOURCES
WHERE        (ServerIdentifier = @ServerIdentifier) AND (SourceIdentifier = @SourceIdentifier) AND (ProviderId = @ProviderId)
GO
PRINT N'Creating [dbo].[MigrationSourceSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceSelectById
--GO

CREATE PROCEDURE dbo.MigrationSourceSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId
FROM            MIGRATION_SOURCES
WHERE        (Id = @Id)
GO
PRINT N'Creating [dbo].[MigrationSourceUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceUpdate
--GO

CREATE PROCEDURE dbo.MigrationSourceUpdate
(
	@FriendlyName nvarchar(128),
	@ServerUrl nvarchar(400),
	@Original_Id int
)
AS
	SET NOCOUNT OFF;
UPDATE       MIGRATION_SOURCES
SET                FriendlyName = @FriendlyName, ServerUrl = @ServerUrl
WHERE        (Id = @Original_Id);
	 
SELECT Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId FROM MIGRATION_SOURCES WHERE (Id = @Original_Id)
GO
PRINT N'Creating [dbo].[NonTransactionDeleteSessionComputedButNotMigratedData]...';


GO
CREATE PROCEDURE [dbo].[NonTransactionDeleteSessionComputedButNotMigratedData]
	@SessionUniqueId UNIQUEIDENTIFIER
AS	
	-- SEE: ReleaseCode\Core\Toolkit\ChangeGroup.cs
	--// these values are persisted in SQL so do not change them between releases
    --/// <summary>
    --/// Change Group Status
    --/// </summary>
    --public enum ChangeStatus
    --{
        --Unintialized = -1,                  // 
        --Delta = 0,                          // Initial state of delta table entry
        --DeltaPending = 1,                   // Group contains delta table entry: NextDeltaTable works on this status
        --DeltaComplete = 2,                  // Group, as delta table entry, is "complete", i.e the entry is no longer needed
        --DeltaSynced = 8,                    // Goup, as delta table entry, has been "synced" to the other side. i.e. the entry is no longer needed for content conflict detection.
        --AnalysisMigrationInstruction = 3,   // The initial status of the migration instruction table entry
        --Pending    = 4,                     // Start processing migration instruction: NextMigrationInstruction works on this status
        --InProgress = 5,                     // 
        --Complete   = 6,                     //
        --Skipped    = 7,                     //
        --ChangeCreationInProgress = 20,      // change group creation is in progress during paged change actions insertion 
        --PendingConflictDetection    = 9,    //
        --Obsolete = 10                       // 
    --}
	
	-- 1. find the "cached" change groups and actions of the sessions
	-- 1.1 delete all "cached" and non-conflicted change actions and groups of the sessions
	-- 1.1.1 find the non-conflicted change group ids for deletion
	DECLARE @ChangeGroupIdsToDelete TABLE(ChangeGroupId BIGINT)
	INSERT INTO @ChangeGroupIdsToDelete
		SELECT G.Id
		FROM [dbo].[RUNTIME_CHANGE_GROUPS] AS G WITH(NOLOCK) 
		WHERE (G.Status = -1 OR G.Status = 0 OR G.Status = 1 OR G.Status = 3 OR G.Status = 4 
			  OR G.Status = 5 OR G.Status = 20 OR G.Status = 9)
		AND G.SessionUniqueId = @SessionUniqueId
		AND G.ContainsBackloggedAction = 0
	
	DECLARE @CountChangeGroupIdsToDelete BIGINT
	SELECT @CountChangeGroupIdsToDelete = COUNT(*) FROM @ChangeGroupIdsToDelete
	IF @CountChangeGroupIdsToDelete = 0
	BEGIN
		RETURN
	END

	-- 1.1.2 delete the change actions
	--------DELETE FROM [dbo].[RUNTIME_CHANGE_ACTION]
	--------WHERE [ChangeGroupId] IN (SELECT ChangeGroupId FROM @ChangeGroupIdsToDelete)
	
	-- 1.1.3 delete the groups and clear the temp table
	--------DELETE FROM [dbo].[RUNTIME_CHANGE_GROUPS]
	--------WHERE Id IN (SELECT ChangeGroupId FROM @ChangeGroupIdsToDelete)
	UPDATE [dbo].[RUNTIME_CHANGE_GROUPS]
	SET Status = 10 -- Obsolete
	WHERE Id IN (SELECT ChangeGroupId FROM @ChangeGroupIdsToDelete)
	
	DELETE FROM @ChangeGroupIdsToDelete
	
	-- 1.2 find the second to the last sync point
	-- NOTE: the last sync point contains the HWM that is updated after last Delta computation
	--       the last change group id in the same sync point also points to the last change group after
	--       the last session trip
	-- WE WANT TO FIND OUT THE SECOND-TO-THE-LAST HWM AND 'LAST CHANGE GROUP ID' to define the scope for deletion
	
	-- find migration source ids for the session
	DECLARE @MigrationSourceIds TABLE(MigrationSourceId UNIQUEIDENTIFIER)
	INSERT INTO @MigrationSourceIds
	SELECT M.UniqueId
	FROM [dbo].[MIGRATION_SOURCES] M WITH(NOLOCK)
	WHERE EXISTS (SELECT * FROM [dbo].[RUNTIME_SESSIONS] S WITH(NOLOCK)
				  WHERE (M.Id = S.LeftSourceId OR M.Id = S.RightSourceId)
				  AND S.SessionUniqueId = @SessionUniqueId)
	
	DECLARE @PerSourceLastSyncPoint TABLE(SourceUniqueId UNIQUEIDENTIFIER, SyncPointId BIGINT)
	INSERT INTO @PerSourceLastSyncPoint
	SELECT S.[SourceUniqueId], MAX(S.Id)
	FROM [dbo].[SYNC_POINT] S WITH(NOLOCK)
	WHERE S.[SessionUniqueId] = @SessionUniqueId
	AND S.[SourceUniqueId] IN (SELECT MigrationSourceId FROM @MigrationSourceIds)
	GROUP BY S.[SourceUniqueId]
	HAVING COUNT(S.Id) > 1 -- a migration source may have only one SyncPoint written
	
	DECLARE @PerSourceSecondToLastSyncPoint TABLE(SourceUniqueId UNIQUEIDENTIFIER, SyncPointId BIGINT)
	INSERT INTO @PerSourceSecondToLastSyncPoint
	SELECT S.[SourceUniqueId], MAX(S.Id)
	FROM [dbo].[SYNC_POINT] S WITH(NOLOCK)
	INNER JOIN @PerSourceLastSyncPoint AS LS 
	ON (LS.SourceUniqueId = S.SourceUniqueId AND LS.SyncPointId > S.Id)
	GROUP BY S.SourceUniqueId
			
    DECLARE @LastChangeGroupIdInPrevSync BIGINT
    DECLARE @CountPerSourceSecondToLastSyncPoint INT
    
	SELECT @CountPerSourceSecondToLastSyncPoint = COUNT(*) 
    FROM @PerSourceSecondToLastSyncPoint
    
    IF @CountPerSourceSecondToLastSyncPoint > 1
    BEGIN
	    -- backward compatibility check: verify if LastChangeGroupId is recorded in the sync point
		SELECT @CountPerSourceSecondToLastSyncPoint = COUNT(*)
		FROM @PerSourceSecondToLastSyncPoint AS P
		INNER JOIN [dbo].[SYNC_POINT] S WITH(NOLOCK) ON S.Id = P.SyncPointId
		WHERE S.LastChangeGroupId IS NOT NULL

		IF @CountPerSourceSecondToLastSyncPoint > 0
		BEGIN
			-- LastChangeGroupId was recorded
			SELECT @LastChangeGroupIdInPrevSync = MIN(S.LastChangeGroupId)
			FROM [dbo].[SYNC_POINT] S WITH(NOLOCK)
			INNER JOIN @PerSourceSecondToLastSyncPoint AS P
			ON P.SyncPointId = S.Id
			WHERE S.LastChangeGroupId IS NOT NULL
		END
		ELSE BEGIN
			-- LastChangeGroupId was NOT recorded, we should NOT delete any cached conflicted data
			SELECT @LastChangeGroupIdInPrevSync = MAX(G.Id)
			FROM [dbo].[RUNTIME_CHANGE_GROUPS] AS G
			WHERE G.SessionUniqueId = @SessionUniqueId

			IF @LastChangeGroupIdInPrevSync IS NULL
			BEGIN
				-- no change group has been created for this session
				SET @LastChangeGroupIdInPrevSync = -1
			END
		END
    END
    ELSE BEGIN
		-- there is totally less than two sync points recorded (for both migration sources), hence after restoring HWM
		-- delta computation will start from the very beginning
		-- Set @LastChangeGroupIdInPrevSync to -1 so that we will clean up all cached data
		SET @LastChangeGroupIdInPrevSync = -1
    END
    
	-- 1.3 delete all "cached" and conflicted change actions and groups (after the last sync-ed group)
	-- 1.3.1 find conflicted groups *after* known last migrated change group
	INSERT INTO @ChangeGroupIdsToDelete
		SELECT G.Id
		FROM [dbo].[RUNTIME_CHANGE_GROUPS] AS G WITH(NOLOCK) 
		WHERE G.SessionUniqueId = @SessionUniqueId
		AND G.Id > @LastChangeGroupIdInPrevSync
		AND (G.Status = -1 OR G.Status = 0 OR G.Status = 1 OR G.Status = 3 OR G.Status = 4 
			 OR G.Status = 5 OR G.Status = 20 OR G.Status = 9)			  
		AND G.ContainsBackloggedAction = 1

	-- 1.3.2 find all change actions to delete
	DECLARE @ChangeActionsIdsToDelete TABLE(ChangeActionId BIGINT)
	INSERT INTO @ChangeActionsIdsToDelete
	SELECT [ChangeActionId] 
	FROM [dbo].[RUNTIME_CHANGE_ACTION] WITH(NOLOCK)
	WHERE [ChangeGroupId] IN (SELECT * FROM @ChangeGroupIdsToDelete)
				
	-- 1.3.3 delete all active conflicts
	--------DELETE FROM [dbo].[CONFLICT_CONFLICTS]
	--------WHERE [ConflictedChangeActionId] IN (SELECT ChangeActionId FROM @ChangeActionsIdsToDelete)
	UPDATE [dbo].[CONFLICT_CONFLICTS]
	SET Status = 1
	WHERE [ConflictedChangeActionId] IN (SELECT ChangeActionId FROM @ChangeActionsIdsToDelete)
	
	-- 1.3.4 delete all conflicted change actions
	--------DELETE FROM [dbo].[RUNTIME_CHANGE_ACTION]
	--------WHERE [ChangeActionId] IN (SELECT ChangeActionId FROM @ChangeActionsIdsToDelete)
	
	-- 1.3.5 delete all conflicted change groups
	--------DELETE FROM [dbo].[RUNTIME_CHANGE_GROUPS]
	--------WHERE Id IN (SELECT ChangeGroupId FROM @ChangeGroupIdsToDelete)
	UPDATE [dbo].[RUNTIME_CHANGE_GROUPS]
	SET Status = 10 -- Obsolete
	WHERE Id IN (SELECT ChangeGroupId FROM @ChangeGroupIdsToDelete)

	-- (skipped) find the "cached" link change groups
	-- NOTE: this step is not needed 
	-- deleting "scoped-out" link is not needed as the link engine will "skip" untranslatable links
	
	-- 2. find the sync point and update hwm for each session
	-- 2.1 if sync point is available, use it
	UPDATE H
	SET [Value] = S.[SourceHighWaterMarkValue]
	FROM [dbo].[HIGH_WATER_MARK] H
	INNER JOIN [dbo].[SYNC_POINT] S
		ON H.[SessionUniqueId] = S.[SessionUniqueId]
		AND H.[SourceUniqueId] = S.[SourceUniqueId]
		AND H.[Name] = S.[SourceHighWaterMarkName]
	INNER JOIN @PerSourceSecondToLastSyncPoint AS P
		ON (P.SourceUniqueId = S.[SourceUniqueId] AND P.SyncPointId = S.Id)
	
	-- 2.2 sources may not have sync point written yet, set their high watermark to NULL
	-- 2.2.1 find all hwm related to the session BUT not having a sync point
	DECLARE @HWMs TABLE(SessionUniqueId UNIQUEIDENTIFIER, SourceUniqueId UNIQUEIDENTIFIER, HWMName NVARCHAR(50))
	INSERT INTO @HWMs 
	SELECT SessionUniqueId, SourceUniqueId, [Name]
	FROM [dbo].[HIGH_WATER_MARK]
	WHERE SessionUniqueId = @SessionUniqueId
	AND NOT EXISTS (SELECT * 
				    FROM [dbo].[HIGH_WATER_MARK] H
				    INNER JOIN @PerSourceSecondToLastSyncPoint AS P
					ON H.[SourceUniqueId] = P.SourceUniqueId)
	-- 2.2.2 update them with default value (NULL)
	update H
	SET [Value] = NULL
	FROM [dbo].[HIGH_WATER_MARK] H
	INNER JOIN @HWMs AS HS
		ON H.[SessionUniqueId] = HS.[SessionUniqueId]
		AND H.[SourceUniqueId] = HS.[SourceUniqueId]
		AND H.[Name] = HS.[HWMName]
	
	-- 3. delete the most recent sync pointS
	DECLARE @PerSourceSyncPointIdToDelete TABLE (Id BIGINT)
	INSERT INTO @PerSourceSyncPointIdToDelete
	SELECT DISTINCT S.Id
	FROM [dbo].[SYNC_POINT] S WITH(NOLOCK)
	INNER JOIN @PerSourceSecondToLastSyncPoint AS P
		ON (P.SourceUniqueId = S.[SourceUniqueId] AND P.SyncPointId < S.Id)

	 -- a migration source may have only one SyncPoint written, and it needs to be deleted
	DELETE FROM @PerSourceSecondToLastSyncPoint
	INSERT INTO @PerSourceSecondToLastSyncPoint
	SELECT S1.[SourceUniqueId], MIN(S1.Id)
	FROM (SELECT TOP(1) S.[SourceUniqueId], S.Id
		  FROM [dbo].[SYNC_POINT] S WITH(NOLOCK)
		  WHERE S.[SessionUniqueId] = @SessionUniqueId
		  AND S.[SourceUniqueId] IN (SELECT MigrationSourceId FROM @MigrationSourceIds)
		  ORDER BY S.Id DESC) AS S1
	GROUP BY S1.[SourceUniqueId]
	HAVING COUNT(S1.Id) = 1

	INSERT INTO @PerSourceSyncPointIdToDelete
	SELECT DISTINCT SyncPointId
	FROM @PerSourceSecondToLastSyncPoint

	DELETE FROM [dbo].[SYNC_POINT] 
	WHERE Id IN (SELECT * FROM @PerSourceSyncPointIdToDelete)
	
	-- 4. delete data in Last Processed Item Version so that delta can be re-generated
	DELETE FROM [dbo].[LAST_PROCESSED_ITEM_VERSIONS]
	WHERE MigrationSourceId IN (SELECT MigrationSourceId FROM @MigrationSourceIds)
RETURN 0
GO
PRINT N'Creating [dbo].[Old_prc_LoadSessionVariable]...';


GO
CREATE PROCEDURE [dbo].[Old_prc_LoadSessionVariable]
	@SessionId nvarchar(max),
	@Variable nvarchar(256)
AS

SELECT TOP 1 Value FROM Old_SessionState
WHERE	SessionId=@SessionId
		AND Variable=@Variable
GO
PRINT N'Creating [dbo].[prc_BatchUpdateChangeGroupsStatus]...';


GO
CREATE PROCEDURE [dbo].[prc_BatchUpdateChangeGroupsStatus]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId  uniqueidentifier,
	@CurrStatus int,
	@NewStatus int
AS
	UPDATE [dbo].[RUNTIME_CHANGE_GROUPS] 
	SET Status = @NewStatus
	WHERE SessionUniqueId = @SessionUniqueId
	AND SourceUniqueId = @SourceUniqueId
	AND Status = @CurrStatus
SELECT * 
	FROM [dbo].[RUNTIME_CHANGE_GROUPS] 
	WHERE SessionUniqueId = @SessionUniqueId
	AND SourceUniqueId = @SourceUniqueId
	AND Status = @CurrStatus
GO
PRINT N'Creating [dbo].[prc_CheckLatency]...';


GO
CREATE PROCEDURE [dbo].[prc_CheckLatency]
	@ThresholdInMinutes int = 30,
	@Subject varchar(128) = 'Sync latency is greater than',
	@SessionGroupId varchar(50) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @UTCOffset int;
	SET @UTCOffset = DATEDIFF (minute, GETUTCDATE(), GETDATE());

	IF @SessionGroupId IS NULL
		SET @SessionGroupId = (SELECT TOP 1 [GroupUniqueId]
	  FROM [Tfs_IntegrationPlatform].[dbo].[SESSION_GROUPS]  
	  -- SessionStatEnum: initialized = 0, running = 1, paused = 2
	where State = 0 or State = 1 or State = 2)
  
	SELECT TOP 1 * FROM
	(
	SELECT  DATEADD(minute, @UTCOffset, [PollTime]) as PollTimePT
		  ,dv.MigrationSourceFriendlyName
		  ,DATEADD(minute, @UTCOffset, [MigrationHWM]) as MigrationHWMPT
		  ,ROUND(CAST(Latency as float) / 60, 0) as LatencyMinutes
		  ,[BacklogCount]
		  ,@Subject + ' ' + CAST(@ThresholdInMinutes as varchar) + ' minutes at ' + CAST(GETDATE() as varchar) as Subject
	  FROM LATENCY_POLL lp
	  LEFT JOIN [Tfs_IntegrationPlatform].[dbo].SessionGroupDetailsView as dv ON lp.MigrationSourceId=dv.MigrationSourceId
	WHERE
		dv.SessionGroupUniqueId= @SessionGroupId
	AND
		lp.Id IN (SELECT MAX(Id) FROM LATENCY_POLL GROUP BY MigrationSourceId)
	) AS blah
	WHERE LatencyMinutes > @ThresholdInMinutes
END
GO
PRINT N'Creating [dbo].[prc_DeleteIncompleteChangeGroups]...';


GO
CREATE PROCEDURE [dbo].[prc_DeleteIncompleteChangeGroups]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId uniqueidentifier
AS
	delete from RUNTIME_CHANGE_ACTION
	where ChangeGroupId in 
		(select Id 
		from RUNTIME_CHANGE_GROUPS
		where SessionUniqueId = @SessionUniqueId 
		and SourceUniqueId = @SourceUniqueId
		and Status = 20); -- 20: ChangeCreationInProgress
	
	delete from RUNTIME_CHANGE_GROUPS
	where SessionUniqueId = @SessionUniqueId 
	and SourceUniqueId = @SourceUniqueId
	and Status = 20; -- 20: ChangeCreationInProgress
	
	SELECT TOP 1 *
	FROM RUNTIME_CHANGE_GROUPS
	where SessionUniqueId = @SessionUniqueId 
	and SourceUniqueId = @SourceUniqueId 

	
RETURN 0;
GO
PRINT N'Creating [dbo].[prc_GetMirroredItemId]...';


GO
CREATE PROCEDURE [dbo].[prc_GetMirroredItemId]
@ItemId varchar(50) = '-1',
@MigrationSourceId int
AS
BEGIN
	declare @MigrationItemInternalId int
	declare @LeftId bigint
	declare @RightId bigint

	SELECT TOP(1) @MigrationItemInternalId=Id
	FROM [dbo].[RUNTIME_MIGRATION_ITEMS] WITH (NOLOCK)
	WHERE (ItemId = @ItemId AND SourceId = @MigrationSourceId)

	IF @MigrationItemInternalId IS NOT NULL
		BEGIN 
			SELECT TOP(1) @LeftId=LeftMigrationItemId, @RightId=RightMigrationItemId
			FROM [dbo].[RUNTIME_ITEM_REVISION_PAIRS] with (NOLOCK) 
			WHERE (LeftMigrationItemId = @MigrationItemInternalId OR RightMigrationItemId = @MigrationItemInternalId)
			
			DECLARE @MirroredItemId bigint
			
			IF (@LeftId = @MigrationItemInternalId)
			BEGIN 
				set @MirroredItemId = @RightId
			END
			ELSE
			BEGIN
				set @MirroredItemId = @LeftId
			END
			
			select ItemId
			FROM [dbo].[RUNTIME_MIGRATION_ITEMS] WITH (NOLOCK)
			WHERE Id = @MirroredItemId
		END
	ELSE
		BEGIN
			select '' as ItemId
		END
END
GO
PRINT N'Creating [dbo].[prc_LoadChangeAction]...';


GO
CREATE PROCEDURE [dbo].[prc_LoadChangeAction]
	@ChangeGroupID bigint
AS

SELECT * FROM [dbo].[RUNTIME_CHANGE_ACTION]
WHERE    ChangeGroupId = @ChangeGroupID
ORDER BY [FromPath] ASC
GO
PRINT N'Creating [dbo].[prc_LoadMigrationSources]...';


GO
CREATE PROCEDURE [dbo].[prc_LoadMigrationSources]
	@SourceUniqueId uniqueidentifier
AS
	SELECT * from [dbo].[MIGRATION_SOURCES] 
	WHERE UniqueId = @SourceUniqueId
GO
PRINT N'Creating [dbo].[prc_PromoteChangeGroups]...';


GO
CREATE PROCEDURE [dbo].[prc_PromoteChangeGroups]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId  uniqueidentifier,
	@CurrentStatus int,
	@NewStatus int
AS
UPDATE [dbo].[RUNTIME_CHANGE_GROUPS]
WITH (UPDLOCK)
SET Status=@NewStatus -- DeltaPending
WHERE SessionUniqueId = @SessionUniqueId
	AND SourceUniqueId = @SourceUniqueId
	AND Status = @CurrentStatus -- Delta
	AND ContainsBackloggedAction = 0
	
SELECT * FROM [dbo].[RUNTIME_CHANGE_GROUPS] 
WHERE SessionUniqueId = @SessionUniqueId
	AND SourceUniqueId = @SourceUniqueId
	AND Status = @NewStatus

RETURN 0;
GO
PRINT N'Creating [dbo].[prc_QueryContentConflict]...';


GO
CREATE PROCEDURE [dbo].[prc_QueryContentConflict]
	@SourceId UNIQUEIDENTIFIER,
	@SessionId UNIQUEIDENTIFIER
AS

CREATE TABLE #Delta (ChangeGroupId bigint, ChangeActionId bigint, ActionId uniqueidentifier, ActionPath nvarchar(max)) -- Temporarily table for delta 
CREATE TABLE #MigrationInstruction (ChangeGroupId bigint, ChangeActionId bigint, ActionId uniqueidentifier, ActionPath nvarchar(max)) -- Temporarily table for migration instruction
CREATE TABLE #ContentConflict (Id bigint IDENTITY(1,1) primary key, DeltaChangeGroupId bigint, DeltaChangeActionId bigint, MigrationInstructionChangeGroupId bigint, MigrationInstructionChangeActionId bigint, ActionPath nvarchar(max)) -- Temporarily table for Content conflicts

-- Select all change actions into #Delta
INSERT INTO #Delta (ChangeGroupId, ChangeActionId, ActionId, ActionPath)
SELECT CA.[ChangeGroupId]
      ,CA.[ChangeActionId]
      ,CA.[ActionId]
      ,CA.[ToPath]
  FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
  RIGHT JOIN RUNTIME_CHANGE_GROUPS AS CG WITH(NOLOCK)
  ON CA.ChangeGroupId = CG.Id 
  WHERE CG.SessionUniqueId = @SessionId AND CG.SourceUniqueId = @SourceId AND CG.Status =1 -- DeltaPending
        AND (CA.ActionId != 'cb71d043-bede-4092-aa87-cf0f14586625' OR CA.ItemTypeReferenceName != 'Microsoft.TeamFoundation.Migration.Toolkit.VersionControlledFolder') -- Add of folder will not be used for conflict detection
  
-- For Rename, also include the rename-from-name
  INSERT INTO #Delta (ChangeGroupId, ChangeActionId, ActionId, ActionPath)
SELECT CA.[ChangeGroupId]
      ,CA.[ChangeActionId]
      ,CA.[ActionId]
      ,CA.[FromPath]
  FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
  RIGHT JOIN RUNTIME_CHANGE_GROUPS AS CG WITH(NOLOCK)
  ON CA.ChangeGroupId = CG.Id 
  WHERE CG.SessionUniqueId = @SessionId AND CG.SourceUniqueId = @SourceId AND CG.Status =1 AND CA.ActionId = '90F9D977-7F2B-4799-9014-786EC62DFC80' -- Rename

-- Select all change actions into #MigrationInstruction
INSERT INTO #MigrationInstruction (ChangeGroupId, ChangeActionId, ActionId, ActionPath)
SELECT CA.[ChangeGroupId]
      ,CA.[ChangeActionId]
      ,CA.[ActionId]
      ,CA.[ToPath]
  FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
  RIGHT JOIN RUNTIME_CHANGE_GROUPS AS CG WITH(NOLOCK)
  ON CA.ChangeGroupId = CG.Id 
  WHERE CG.SessionUniqueId = @SessionId AND CG.SourceUniqueId = @SourceId AND CG.Status = 9 -- PendingConflictDetection
 
 -- For Rename, also include the rename-from-name 
  INSERT INTO #MigrationInstruction (ChangeGroupId, ChangeActionId, ActionId, ActionPath)
SELECT CA.[ChangeGroupId]
      ,CA.[ChangeActionId]
      ,CA.[ActionId]
      ,CA.[FromPath]
  FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
  RIGHT JOIN RUNTIME_CHANGE_GROUPS AS CG WITH(NOLOCK)
  ON CA.ChangeGroupId = CG.Id 
  WHERE CG.SessionUniqueId = @SessionId AND CG.SourceUniqueId = @SourceId AND CG.Status = 9 and CA.ActionId = '90F9D977-7F2B-4799-9014-786EC62DFC80' -- Rename

-- Detect conflicts
INSERT INTO #ContentConflict(DeltaChangeGroupId, DeltaChangeActionId, MigrationInstructionChangeGroupId, MigrationInstructionChangeActionId, ActionPath)
SELECT 
#Delta.ChangeGroupId,
#Delta.ChangeActionId, 
#MigrationInstruction.ChangeGroupId,
#MigrationInstruction.ChangeActionId,
#Delta.ActionPath
FROM #Delta
INNER JOIN #MigrationInstruction
ON #Delta.ActionPath = #MigrationInstruction.ActionPath

-- Update all changegroups ContainsBackloggedAction
UPDATE RUNTIME_CHANGE_GROUPS
SET ContainsBackloggedAction =1
FROM RUNTIME_CHANGE_GROUPS AS CG
JOIN #ContentConflict AS Conflict
ON CG.Id = Conflict.MigrationInstructionChangeGroupId

--SELECT 
--CA.ChangeActionId,
--CA.ChangeGroupId,
--CA.ActionComment,
--CA.ActionData,
--CA.ActionId,
--CA.AnalysisPhase,
--CA.Backlogged,
--CA.ExecutionOrder,
--CA.FinishTime,
--CA.FromPath,
--CA.IsSubstituted,
--CA.ItemTypeReferenceName,
--CA.Label,
--CA.MergeVersionTo,
--CA.Recursivity,
--CA.SourceItem,
--CA.StartTime,
--CA.ToPath,
--CA.Version
--FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
--INNER JOIN #ContentConflict AS Conflict
--ON CA.ChangeActionId = Conflict.MigrationInstructionChangeActionId

SELECT 
  Conflict.Id,
  Conflict.MigrationInstructionChangeActionId
  ,Conflict.DeltaChangeActionId
FROM #ContentConflict AS Conflict
ORDER BY Id ASC
GO
PRINT N'Creating [dbo].[prc_QueryConversionHistory]...';


GO
CREATE PROCEDURE [dbo].[prc_QueryConversionHistory]
	@SessionId UNIQUEIDENTIFIER,
	@SourceUniqueId UNIQUEIDENTIFIER,
	@OtherSideChangeId NVARCHAR(50)
AS
	SELECT * FROM [dbo].[RUNTIME_CONVERSION_HISTORY]
	--WHERE [SessionUniqueId]=@SessionId AND
		  --[SourceUniqueId]=@SourceUniqueId AND
		  --[OtherSideChangeId]=@OtherSideChangeId
GO
PRINT N'Creating [dbo].[prc_ResetChangeGroupsAfterResolve]...';


GO
CREATE PROCEDURE [dbo].[prc_ResetChangeGroupsAfterResolve]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId uniqueidentifier,
	@DeltaTableExecutionOrder bigint
AS
-- Reset all later delta table entries back to deltapending	
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 1 -- DeltaPending
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND ExecutionOrder > @DeltaTableExecutionOrder
	
-- Reset all unprocessed migration instructions
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 10 -- Obsolete
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND Status = 4 OR Status = 5 OR Status = 9 -- Pending or InProgress or PendingConflictDetection
	
	SELECT * FROM RUNTIME_CHANGE_GROUPS
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND ExecutionOrder = @DeltaTableExecutionOrder

RETURN 0;
GO
PRINT N'Creating [dbo].[prc_Stage1DBCleanup]...';


GO
--Stage One DB cleanup: remove the completed migration instruction ChangeActions

CREATE PROCEDURE [dbo].[prc_Stage1DBCleanup]
AS
	DECLARE @DeletionChangeActionId TABLE(Id BIGINT)

	-- RETRIEVE FIRST BATCH
	INSERT INTO @DeletionChangeActionId(Id)
	SELECT TOP 10000 a.ChangeActionId
	FROM dbo.RUNTIME_CHANGE_ACTION AS a
	INNER JOIN dbo.RUNTIME_CHANGE_GROUPS AS g
	   ON g.Id = a.ChangeGroupId
	WHERE (g.Status = 6 AND g.ContainsBackloggedAction = 0)


	-- SKIP DELETION IF THERE ARE NOT *MANY* JUNK IN DB
	WHILE (SELECT COUNT(*) FROM @DeletionChangeActionId) = 10000
	BEGIN
		PRINT CONVERT(VARCHAR(12),GETDATE(),114) + N' - Deleting 10000 RUNTIME_CHANGE_ACTION rows'
		DELETE FROM dbo.RUNTIME_CHANGE_ACTION
		WHERE ChangeActionId IN (SELECT Id FROM @DeletionChangeActionId)
		
		-- RETRIEVE NEXT BATCH
		DELETE FROM @DeletionChangeActionId	
		
		INSERT INTO @DeletionChangeActionId(Id)
		SELECT TOP 10000 a.ChangeActionId
		FROM dbo.RUNTIME_CHANGE_ACTION AS a
		INNER JOIN dbo.RUNTIME_CHANGE_GROUPS AS g
		   ON g.Id = a.ChangeGroupId
		WHERE (g.Status = 6 AND g.ContainsBackloggedAction = 0)	
	END
RETURN 0;
GO
PRINT N'Creating [dbo].[prc_UpdateChangeGroupStatus]...';


GO
CREATE PROCEDURE [dbo].[prc_UpdateChangeGroupStatus]
	@Id int, 
	@NewStatus int
AS
	UPDATE [dbo].[RUNTIME_CHANGE_GROUPS] 
	SET Status = @NewStatus
	WHERE Id = @Id
SELECT * from [dbo].[RUNTIME_CHANGE_GROUPS] 
WHERE Id = @Id
GO
PRINT N'Creating [dbo].[prc_UpdateSessionVariable]...';


GO
CREATE PROCEDURE prc_UpdateSessionVariable
	@SessionId nvarchar(max),
	@Variable nvarchar(256),
	@Value nvarchar(max)
AS

IF EXISTS(
	SELECT TOP 1 * FROM Old_SessionState
	WHERE	SessionId=@SessionId
			AND Variable=@Variable
	)
	BEGIN
		UPDATE Old_SessionState
		SET Value=@Value
		WHERE	SessionId=@SessionId
				AND Variable=@Variable 
	END
	ELSE
	BEGIN
		INSERT INTO Old_SessionState VALUES(@SessionId, @Variable, @Value)
	END
GO
PRINT N'Creating [dbo].[ProviderInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderInsert
--GO

CREATE PROCEDURE dbo.ProviderInsert
(
	@ReferenceName uniqueidentifier,
	@FriendlyName nvarchar(128)
)
AS
	SET NOCOUNT OFF;
INSERT INTO PROVIDERS
                         (ReferenceName, FriendlyName)
VALUES        (@ReferenceName,@FriendlyName);
	 
SELECT Id, ReferenceName, FriendlyName FROM PROVIDERS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Creating [dbo].[ProviderSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderSelectById
--GO

CREATE PROCEDURE dbo.ProviderSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, ReferenceName, FriendlyName
FROM            PROVIDERS
WHERE        (Id = @Id)
GO
PRINT N'Creating [dbo].[ProviderSelectByRegFileName]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderSelectByRegFileName' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderSelectByRegFileName
--GO

CREATE PROCEDURE dbo.ProviderSelectByRegFileName
(
	@ReferenceName uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, ReferenceName, FriendlyName
FROM            PROVIDERS
WHERE        (ReferenceName = @ReferenceName)
GO
PRINT N'Creating [dbo].[ProviderUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderUpdate
--GO

CREATE PROCEDURE dbo.ProviderUpdate
(
	@FriendlyName nvarchar(128),
	@Original_Id int,
	@Original_ReferenceName uniqueidentifier
)
AS
	SET NOCOUNT OFF;
UPDATE       PROVIDERS
SET                FriendlyName = @FriendlyName
WHERE        (Id = @Original_Id) AND (ReferenceName = @Original_ReferenceName);
	 
SELECT Id, ReferenceName, FriendlyName FROM PROVIDERS WHERE (Id = @Original_Id)
GO
PRINT N'Creating [dbo].[SessionConfigurationInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationInsert
--GO

CREATE PROCEDURE dbo.SessionConfigurationInsert
(
	@SessionUniqueId uniqueidentifier,
	@FriendlyName nvarchar(128),
	@SessionGroupConfigId int,
	@CreationTime datetime,
	@Creator nvarchar(50),
	@DeprecationTime datetime,
	@LeftSourceConfigId int,
	@RightSourceConfigId int,
	@Type int,
	@SettingXml xml,
	@SettingXmlSchema xml
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[SESSION_CONFIGURATIONS] ([SessionUniqueId], [FriendlyName], [SessionGroupConfigId], [CreationTime], [Creator], [DeprecationTime], [LeftSourceConfigId], [RightSourceConfigId], [Type], [SettingXml], [SettingXmlSchema]) VALUES (@SessionUniqueId, @FriendlyName, @SessionGroupConfigId, @CreationTime, @Creator, @DeprecationTime, @LeftSourceConfigId, @RightSourceConfigId, @Type, @SettingXml, @SettingXmlSchema);
	
SELECT Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, SettingXml, SettingXmlSchema FROM SESSION_CONFIGURATIONS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Creating [dbo].[SessionConfigurationSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectById
--GO

CREATE PROCEDURE dbo.SessionConfigurationSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (Id = @Id)
GO
PRINT N'Creating [dbo].[SessionConfigurationSelectBySessionGroupConfigId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectBySessionGroupConfigId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectBySessionGroupConfigId
--GO

CREATE PROCEDURE dbo.SessionConfigurationSelectBySessionGroupConfigId
(
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (SessionGroupConfigId = @SessionGroupConfigId)
GO
PRINT N'Creating [dbo].[SessionConfigurationSelectBySessionUniqueId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectBySessionUniqueId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectBySessionUniqueId
--GO

CREATE PROCEDURE dbo.SessionConfigurationSelectBySessionUniqueId
(
	@SessionUniqueId uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (SessionUniqueId = @SessionUniqueId)
GO
PRINT N'Creating [dbo].[SessionGroupConfigInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigInsert
--GO

CREATE PROCEDURE dbo.SessionGroupConfigInsert
(
	@CreationTime datetime,
	@Creator nvarchar(50),
	@DeprecationTime datetime,
	@Status int,
	@SessionGroupId int,
	@LinkingSettingId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO SESSION_GROUP_CONFIGS
                         (CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId)
VALUES        (@CreationTime,@Creator,@DeprecationTime,@Status,@SessionGroupId,@LinkingSettingId);
	 
SELECT Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId FROM SESSION_GROUP_CONFIGS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Creating [dbo].[SessionGroupConfigSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectById
--GO

CREATE PROCEDURE dbo.SessionGroupConfigSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (Id = @Id)
GO
PRINT N'Creating [dbo].[SessionGroupConfigSelectBySessionGroupId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectBySessionGroupId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectBySessionGroupId
--GO

CREATE PROCEDURE dbo.SessionGroupConfigSelectBySessionGroupId
(
	@SessionGroupId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (SessionGroupId = @SessionGroupId)
GO
PRINT N'Creating [dbo].[SessionGroupConfigSelectByStatusAndSessionGroup]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectByStatusAndSessionGroup' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectByStatusAndSessionGroup
--GO

CREATE PROCEDURE dbo.SessionGroupConfigSelectByStatusAndSessionGroup
(
	@SessionGroupId int,
	@Status int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (SessionGroupId = @SessionGroupId) AND (Status = @Status)
GO
PRINT N'Creating [dbo].[SessionGroupInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupInsert
--GO

CREATE PROCEDURE dbo.SessionGroupInsert
(
	@GroupUniqueId uniqueidentifier,
	@FriendlyName nvarchar(128)
)
AS
	SET NOCOUNT OFF;
INSERT INTO SESSION_GROUPS
                         (GroupUniqueId, FriendlyName)
VALUES        (@GroupUniqueId,@FriendlyName);
	 
SELECT Id, GroupUniqueId, FriendlyName FROM SESSION_GROUPS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Creating [dbo].[SessionGroupSelectByGroupUniqueId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupSelectByGroupUniqueId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupSelectByGroupUniqueId
--GO

CREATE PROCEDURE dbo.SessionGroupSelectByGroupUniqueId
(
	@GroupUniqueId uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, GroupUniqueId, FriendlyName
FROM            SESSION_GROUPS
WHERE        (GroupUniqueId = @GroupUniqueId)
GO
PRINT N'Creating [dbo].[SessionGroupSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupSelectById
--GO

CREATE PROCEDURE dbo.SessionGroupSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, GroupUniqueId, FriendlyName
FROM            SESSION_GROUPS
WHERE        (Id = @Id)
GO
PRINT N'Creating [dbo].[SessionGroupUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupUpdate
--GO

CREATE PROCEDURE dbo.SessionGroupUpdate
(
	@FriendlyName nvarchar(128),
	@Original_Id int,
	@Original_GroupUniqueId uniqueidentifier
)
AS
	SET NOCOUNT OFF;
UPDATE       SESSION_GROUPS
SET                FriendlyName = @FriendlyName
WHERE        (Id = @Original_Id) AND (GroupUniqueId = @Original_GroupUniqueId);
	 
SELECT Id, GroupUniqueId, FriendlyName FROM SESSION_GROUPS WHERE (Id = @Original_Id)
GO
PRINT N'Creating [dbo].[StoredCredentialInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialInsert
--GO

CREATE PROCEDURE dbo.StoredCredentialInsert
(
	@CredentialString nvarchar(300),
	@MigrationSourceId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO STORED_CREDENTIALS
                         (CredentialString, MigrationSourceId)
VALUES        (@CredentialString,@MigrationSourceId);
	 
SELECT Id, CredentialString, MigrationSourceId FROM STORED_CREDENTIALS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Creating [dbo].[StoredCredentialSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialSelectById
--GO

CREATE PROCEDURE dbo.StoredCredentialSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CredentialString, MigrationSourceId
FROM            STORED_CREDENTIALS
WHERE        (Id = @Id)
GO
PRINT N'Creating [dbo].[StoredCredentialSelectByMigrationSourceId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialSelectByMigrationSourceId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialSelectByMigrationSourceId
--GO

CREATE PROCEDURE dbo.StoredCredentialSelectByMigrationSourceId
(
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CredentialString, MigrationSourceId
FROM            STORED_CREDENTIALS
WHERE        (MigrationSourceId = @MigrationSourceId)
GO
PRINT N'Creating [dbo].[StoredCredentialUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialUpdate
--GO

CREATE PROCEDURE dbo.StoredCredentialUpdate
(
	@CredentialString nvarchar(300),
	@Original_MigrationSourceId int
)
AS
	SET NOCOUNT OFF;
UPDATE       STORED_CREDENTIALS
SET                CredentialString = @CredentialString
WHERE        (MigrationSourceId = @Original_MigrationSourceId);
	 
SELECT Id, CredentialString, MigrationSourceId FROM STORED_CREDENTIALS WHERE (MigrationSourceId = @Original_MigrationSourceId)
GO
PRINT N'Creating [dbo].[TransactionalDeleteSessionComputedButNotMigratedData]...';


GO
CREATE PROCEDURE [dbo].[TransactionalDeleteSessionComputedButNotMigratedData]
	@SessionUniqueId UNIQUEIDENTIFIER
AS
	-- PROTECTION AGAINST RUNTIME/TIMEOUT ERROR
	SET XACT_ABORT ON 
	 
	BEGIN TRANSACTION DeleteComputedDataTransaction;	
	BEGIN TRY
	
		EXECUTE [dbo].[NonTransactionDeleteSessionComputedButNotMigratedData] @SessionUniqueId;
		
		PRINT N'Committing...';
		COMMIT TRANSACTION DeleteComputedDataTransaction;
		PRINT N'Committed...';
	
	END TRY	
	BEGIN CATCH
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT @ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		IF @@TRANCOUNT > 0
			PRINT N'Starting Rollback...';
			ROLLBACK TRANSACTION DeleteComputedDataTransaction;
			RAISERROR (@ErrorMessage,@ErrorSeverity, @ErrorState)
	END CATCH;	
	
	SELECT TOP 1 * FROM [dbo].[RUNTIME_SESSIONS]
	WHERE SessionUniqueId = @SessionUniqueId
		
RETURN 0
GO
PRINT N'Creating [dbo].[UpdateMigrationSessionStatusToCompleted]...';


GO
CREATE PROCEDURE [dbo].[UpdateMigrationSessionStatusToCompleted]
	@SessionId uniqueidentifier
AS
	DECLARE @SessionInternalId int;
	DECLARE @SessionGroupInternalId int;
	DECLARE @NumOfUncompletedSession int;
	
	SELECT @SessionInternalId = Id
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE SessionUniqueId = @SessionId;
	
	IF @SessionInternalId IS NULL
	BEGIN
		RETURN 0;
	END
		
	UPDATE [dbo].[RUNTIME_SESSIONS]
	SET State = 3
	WHERE Id = @SessionInternalId;
	
	SELECT @SessionGroupInternalId = SessionGroupId
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE Id = @SessionInternalId;
	
	SELECT @NumOfUncompletedSession = COUNT(*)
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE SessionGroupId = @SessionGroupInternalId
	  AND State <> 3;
	  
	IF @NumOfUncompletedSession = 0
	BEGIN
		UPDATE [dbo].[SESSION_GROUPS]
		SET State = 3
		WHERE Id = @SessionGroupInternalId;
	END
	
	SELECT *
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE Id = @SessionInternalId;
	
RETURN 0;
GO
PRINT N'Creating [dbo].[CreateItemRevisionPair]...';


GO
CREATE PROCEDURE [dbo].[CreateItemRevisionPair]
	@ConversionHistoryId BIGINT,
	@SourceMigrationId INT, 
	@SourceChangeId NVARCHAR(50), 
	@SourceChangeVersion NVARCHAR(50), 
	@OthereSideMigrationId INT,
	@OtherSideChangeId NVARCHAR(50), 
	@OtherSideChangeVersion NVARCHAR(50)
AS
	DECLARE @SourceItemId BIGINT;
	DECLARE @TargetItemId BIGINT;
	DECLARE @ItemRevPairCount INT;
	
	EXECUTE FindCreateMigrationItem @SourceMigrationId, @SourceChangeId, @SourceChangeVersion, @SourceItemId output;
	EXECUTE FindCreateMigrationItem @OthereSideMigrationId, @OtherSideChangeId, @OtherSideChangeVersion, @TargetItemId output;
	
	SELECT @ItemRevPairCount = COUNT(*)
	FROM RUNTIME_ITEM_REVISION_PAIRS
	WHERE ConversionHistoryId = @ConversionHistoryId
	  AND LeftMigrationItemId = @SourceItemId
	  AND RightMigrationItemId = @TargetItemId;
	  
	IF @ItemRevPairCount > 0 BEGIN
		SELECT @ItemRevPairCount = COUNT(*)
		FROM RUNTIME_ITEM_REVISION_PAIRS
		WHERE ConversionHistoryId = @ConversionHistoryId
		  AND LeftMigrationItemId = @TargetItemId
		  AND RightMigrationItemId = @SourceItemId;
	  
	    IF @ItemRevPairCount > 0 BEGIN
			INSERT INTO RUNTIME_ITEM_REVISION_PAIRS(LeftMigrationItemId, RightMigrationItemId, ConversionHistoryId)
				VALUES (@SourceItemId, @TargetItemId, @ConversionHistoryId);
		END
	END
RETURN 0;
GO
PRINT N'Creating [dbo].[DeleteComputedButNotMigratedData]...';


GO
CREATE PROCEDURE [dbo].[DeleteComputedButNotMigratedData]
	@SessionGroupUniqueId UNIQUEIDENTIFIER
AS
	-- PROTECTION AGAINST RUNTIME/TIMEOUT ERROR
	SET XACT_ABORT ON 
	 
	BEGIN TRANSACTION DeleteComputedDataTransaction;	
	BEGIN TRY
	
		-- 1. find the sessions in the session group
		DECLARE @SessionIds TABLE(SessionUniqueId UNIQUEIDENTIFIER)
		INSERT INTO @SessionIds(SessionUniqueId)
			SELECT DISTINCT S.SessionUniqueId
			FROM [dbo].[RUNTIME_SESSIONS] AS S WITH(NOLOCK)
			INNER JOIN [dbo].[SESSION_GROUPS] AS G WITH(NOLOCK) ON S.SessionGroupId = G.Id
			WHERE G.GroupUniqueId = @SessionGroupUniqueId
		
		DECLARE @SessionUniqueId UNIQUEIDENTIFIER
		DECLARE session_cursor CURSOR FOR SELECT SessionUniqueId FROM @SessionIds
		OPEN session_cursor 
		FETCH NEXT FROM session_cursor INTO @SessionUniqueId

		WHILE @@FETCH_STATUS = 0   
		BEGIN   
			EXECUTE [dbo].[NonTransactionDeleteSessionComputedButNotMigratedData] @SessionUniqueId;
			FETCH NEXT FROM session_cursor INTO @SessionUniqueId
		END   
		
		PRINT N'Committing...';
		COMMIT TRANSACTION DeleteComputedDataTransaction;
		PRINT N'Committed...';
	
	END TRY	
	BEGIN CATCH
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT @ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		IF @@TRANCOUNT > 0
			PRINT N'Starting Rollback...';
			ROLLBACK TRANSACTION DeleteComputedDataTransaction;
			RAISERROR (@ErrorMessage,@ErrorSeverity, @ErrorState)
	END CATCH;
	
	SELECT TOP 1 * FROM [dbo].[SESSION_GROUPS]
	WHERE GroupUniqueId = @SessionGroupUniqueId
	
RETURN 0
GO
PRINT N'Creating [dbo].[FindCreateLink]...';


GO
CREATE PROCEDURE [dbo].[FindCreateLink]
	--@SessionGroupUniqueId uniqueidentifier,
	--@SessionUniqueId uniqueidentifier,
	--@SourceID uniqueidentifier,
	--@ActionId uniqueidentifier,
	@SourceArtifactId nvarchar(400),
	@SourceArtifactUri nvarchar(400),
	@TargetArtifactUri nvarchar(400),
	@Comment nvarchar(max),
	@LinkTypeReferenceName nvarchar(200),
	@LinkTypeDisplayName nvarchar(200),
	@ExtendedLinkProperty nvarchar(max),
	@SourceArtifactTypeReferenceName nvarchar(200),
	@SourceArtifactTypeDisplayName nvarchar(200),
	@SourceArtifactContentType nvarchar(400),
	@TargetArtifactTypeReferenceName nvarchar(200),
	@TargetArtifactTypeDisplayName nvarchar(200),
	@TargetArtifactContentType nvarchar(400),
	@CreateOnMissing bit
AS
    declare @LinkChangeActionId int;
    declare @SourceArtifactTypeId int;
    declare @TargetArtifactTypeId int;
    declare @LinkTypeId int;
    declare @ArtifactLinkId int;
    
    execute FindCreateArtifactType 
		@SourceArtifactTypeReferenceName, 
		@SourceArtifactTypeDisplayName, 
		@SourceArtifactContentType,
		@SourceArtifactTypeId output;
	
	execute FindCreateArtifactType
		@TargetArtifactTypeReferenceName,
		@TargetArtifactTypeDisplayName,
		@TargetArtifactContentType,
		@TargetArtifactTypeId output;
	
	execute FindCreateLinkType
		@LinkTypeReferenceName,
		@LinkTypeDisplayName,
		@SourceArtifactTypeId,
		@TargetArtifactTypeId,
		@ExtendedLinkProperty,
		@LinkTypeId output;
		
	execute FindCreateArtifactLink
		@SourceArtifactUri,
		@TargetArtifactUri,
		@LinkTypeId,
		@Comment,
		@SourceArtifactId,
		@CreateOnMissing,
		@ArtifactLinkId output;
		
	--select @LinkChangeActionId = Id
	--from LINK_LINK_CHANGE_ACTIONS
	--where SessionGroupUniqueId = @SessionGroupUniqueId
		--and SessionUniqueId = @SessionUniqueId
		--and SourceId = @SourceID
		--and ActionId = @ActionId
		--and ArtifactLinkId = @ArtifactLinkId;
	
	--if @LinkChangeActionId is null
	--begin
		--insert into LINK_LINK_CHANGE_ACTIONS(SessionGroupUniqueId, SessionUniqueId, SourceId, ActionId, ArtifactLinkId, Status, Conflicted)
		--values(@SessionGroupUniqueId, @SessionUniqueId, @SourceID, @ActionId, @ArtifactLinkId, 1, 0); -- default to DeltaComputed and non-conflicted state
		--select @LinkChangeActionId = @@identity
	--end
	
	--SELECT *
	--FROM LINK_LINK_CHANGE_ACTIONS
	--WHERE Id=@LinkChangeActionId
	
	SELECT *
	FROM LINK_ARTIFACT_LINK
	WHERE Id = @ArtifactLinkId
RETURN 0;
GO
PRINT N'Creating [dbo].[prc_UpdateConversionHistory]...';


GO
CREATE PROCEDURE [dbo].[prc_UpdateConversionHistory]
	@SessionRunId int, 
	@SourceMigrationId int, 
	@OtherSideMigrationId int,
	@SourceChangeGroupId bigint , 
	@SourceChangeId NVARCHAR(50), 
	@SourceChangeVersion NVARCHAR(50), 
	@OtherSideChangeId NVARCHAR(50), 
	@OtherSideChangeVersion NVARCHAR(50), 
	@ExecutionOrder bigint,
	@UtcWhen DATETIME,
	@Comment NVARCHAR(MAX)
AS
-- Insert into conversion history
	DECLARE @ConvHistoryId BIGINT;
	INSERT INTO dbo.RUNTIME_CONVERSION_HISTORY (SessionRunId, SourceMigrationSourceId, SourceChangeGroupId, UtcWhen, Comment)
		VALUES (@SessionRunId, @SourceMigrationId, @SourceChangeGroupId, @UtcWhen, @Comment)
	SELECT @ConvHistoryId = @@IDENTITY;
	
	EXECUTE CreateItemRevisionPair @ConvHistoryId, @SourceMigrationId, @SourceChangeId, @SourceChangeVersion, 
								   @OtherSideMigrationId, @OtherSideChangeId, @OtherSideChangeVersion;
	
-- Mark the delta table entry as delta synced	
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 8 -- DeltaSynced
	WHERE SessionRunId = @SessionRunId AND SourceMigrationSourceId <> @SourceMigrationId AND ExecutionOrder = @ExecutionOrder
	
	SELECT top(1) * FROM dbo.RUNTIME_CONVERSION_HISTORY
	WHERE SessionRunId = @SessionRunId AND SourceMigrationSourceId = @SourceMigrationId
RETURN 0;
GO
PRINT N'Creating [dbo].[MigrationSourceNotUsedInExistingSessions]...';


GO
CREATE FUNCTION [dbo].[MigrationSourceNotUsedInExistingSessions]
(
	@LeftSourceId int, 
	@RightSourceId int
)
RETURNS INT -- 0 if neither @LeftSourceId or @RightSourceId is used in an existing Session
            -- 1 in other cases
AS
BEGIN
	DECLARE @LeftSourceUsageCount INT
	DECLARE @RightSourceUsageCount INT
	DECLARE @RetVal INT
	
	SELECT @LeftSourceUsageCount = COUNT(*) 
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE RightSourceId = @LeftSourceId
	   OR LeftSourceId = @LeftSourceId
	
	-- the only usage should be the session row that this function is checking in CHECK CONSTRAINT
	IF @LeftSourceUsageCount > 1 
	BEGIN
		-- @LeftSourceId is already used by an existing session
		SET @RetVal = 1
	END
	ELSE
	BEGIN
		SELECT @RightSourceUsageCount = COUNT(*) 
		FROM [dbo].[RUNTIME_SESSIONS]
		WHERE RightSourceId = @RightSourceId
		   OR LeftSourceId = @RightSourceId
	
		-- the only usage should be the session row that this function is checking in CHECK CONSTRAINT	   
		IF @RightSourceUsageCount > 1
		BEGIN
			-- @RightSourceId is already used by an existing session
			SET @RetVal = 1
		END
		ELSE
		BEGIN
			SET @RetVal = 0
		END
	END
	
	RETURN @RetVal
END
GO
PRINT N'Creating chkSingleUsageOfMigrationSourceInSessions...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH NOCHECK
    ADD CONSTRAINT [chkSingleUsageOfMigrationSourceInSessions] CHECK ([dbo].[MigrationSourceNotUsedInExistingSessions](LeftSourceId, RightSourceId) = 0);


GO
PRINT N'Creating [dbo].[SessionGroupDetailsView]...';


GO
CREATE VIEW [dbo].[SessionGroupDetailsView]
	AS 
SELECT sData.SessionGroupUniqueId AS SessionGroupUniqueId
      ,sData.SessionGroupFriendlyName AS SessionGroupFriendlyName
      ,sData.SessionUniqueId AS SessionUniqueId
      ,sData.SessionFriendlyName AS SessionFriendlyName
      ,sData.SessionType As SessionType
      ,mData.MigrationSourceId AS MigrationSourceId
      ,mData.MigrationSourceUniqueId AS MigrationSourceUniqueId
      ,mData.MigrationSourceFriendlyName As MigrationSourceFriendlyName
FROM
(SELECT SessionData.SessionGroupUniqueId AS SessionGroupUniqueId
       ,SessionData.SessionGroupFriendlyName AS SessionGroupFriendlyName
       ,SessionData.SessionUniqueId AS SessionUniqueId
       ,sc.FriendlyName AS SessionFriendlyName
       ,sc.Type As SessionType
 FROM
  (SELECT sg.[GroupUniqueId] AS SessionGroupUniqueId
      ,sg.[FriendlyName] AS SessionGroupFriendlyName
      ,s.SessionUniqueId AS SessionUniqueId
  FROM [dbo].[SESSION_GROUPS] AS sg
  INNER JOIN [dbo].[RUNTIME_SESSIONS] AS s ON sg.Id = s.SessionGroupId
  GROUP BY sg.[GroupUniqueId]
      ,sg.[FriendlyName]
      ,s.SessionUniqueId) AS SessionData
INNER JOIN [dbo].[SESSION_CONFIGURATIONS] AS sc ON sc.SessionUniqueId = SessionData.SessionUniqueId) As sData
INNER JOIN
(SELECT s.SessionUniqueId AS SessionUniqueId      
      ,ms.Id AS MigrationSourceId
      ,ms.UniqueId AS MigrationSourceUniqueId
      ,ms.FriendlyName As MigrationSourceFriendlyName
  FROM [dbo].[RUNTIME_SESSIONS] AS s
  INNER JOIN [dbo].[MIGRATION_SOURCES] AS ms ON (ms.Id = s.LeftSourceId OR ms.Id = s.RightSourceId)
  GROUP BY s.SessionUniqueId
      ,ms.Id
      ,ms.UniqueId
      ,ms.FriendlyName) AS mData
ON sData.SessionUniqueId = mData.SessionUniqueId
GROUP BY sData.SessionGroupUniqueId
      ,sData.SessionGroupFriendlyName
      ,sData.SessionUniqueId
      ,sData.SessionFriendlyName
      ,sData.SessionType
      ,mData.MigrationSourceId
      ,mData.MigrationSourceUniqueId
      ,mData.MigrationSourceFriendlyName
GO
PRINT N'Creating [FriendlyName]...';


GO
EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Integration Platform Database v2.10';


GO
PRINT N'Creating [ReferenceName]...';


GO
EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = 'FC4AE969-B5A7-4A03-ADF1-FDDCAFB6FFD0';


GO
-- Refactoring step to update target server with deployed transaction logs
CREATE TABLE  [dbo].[__RefactorLog] (OperationKey UNIQUEIDENTIFIER NOT NULL PRIMARY KEY)
GO
sp_addextendedproperty N'microsoft_database_tools_support', N'refactoring log', N'schema', N'dbo', N'table', N'__RefactorLog'
GO

GO
-- Project upgrade has moved this code to 'Upgraded.extendedproperties.sql'.

GO
PRINT N'Checking existing data against newly created constraints';


GO
USE [Tfs_IntegrationPlatform];


GO
ALTER TABLE [dbo].[CONFIG_CHECKOUT_RECORDS] WITH CHECK CHECK CONSTRAINT [FK_ConfigCheckoutRecords];

ALTER TABLE [dbo].[CONFLICT_CONFLICT_TYPES] WITH CHECK CHECK CONSTRAINT [FK_to_provider];

ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH CHECK CHECK CONSTRAINT [FK_Conflicts_to_ChangeAction];

ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH CHECK CHECK CONSTRAINT [FK_Conflicts_to_ConflictCollection];

ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH CHECK CHECK CONSTRAINT [FK_Conflicts_to_ConflictType];

ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH CHECK CHECK CONSTRAINT [FK_Conflicts_to_LinkChangeAction];

ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH CHECK CHECK CONSTRAINT [FK_Conflicts_to_LinkChangeGroup];

ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH CHECK CHECK CONSTRAINT [FK_Conflicts_to_MigrationSource];

ALTER TABLE [dbo].[CONFLICT_CONFLICTS] WITH CHECK CHECK CONSTRAINT [FK_Conflicts_to_ResolveRule];

ALTER TABLE [dbo].[CONFLICT_CONTENT_RESV] WITH CHECK CHECK CONSTRAINT [FK_ContentResv1];

ALTER TABLE [dbo].[CONFLICT_CONTENT_RESV] WITH CHECK CHECK CONSTRAINT [FK_ContentResv2];

ALTER TABLE [dbo].[CONFLICT_RESOLUTION_ACTIONS] WITH CHECK CHECK CONSTRAINT [FK_action_to_provider];

ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES] WITH CHECK CHECK CONSTRAINT [FK_ResolutionRule_to_ResolutionAction];

ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES] WITH CHECK CHECK CONSTRAINT [FK_ResolutionRules1];

ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES] WITH CHECK CHECK CONSTRAINT [FK_ResolutionRules2];

ALTER TABLE [dbo].[EVENT_SINK] WITH CHECK CHECK CONSTRAINT [FK_EventSink];

ALTER TABLE [dbo].[EVENT_SINK_JUNC] WITH CHECK CHECK CONSTRAINT [FK_EventSinkJunc1];

ALTER TABLE [dbo].[EVENT_SINK_JUNC] WITH CHECK CHECK CONSTRAINT [FK_EventSinkJunc2];

ALTER TABLE [dbo].[FIELD_EXCLUSION_COLLECTION] WITH CHECK CHECK CONSTRAINT [FK_FieldExclCollection];

ALTER TABLE [dbo].[FIELD_MAP_COLLECTION] WITH CHECK CHECK CONSTRAINT [FK_FieldMapCollection1];

ALTER TABLE [dbo].[FIELD_MAP_COLLECTION] WITH CHECK CHECK CONSTRAINT [FK_FieldMapCollection2];

ALTER TABLE [dbo].[FILTER_ITEM_PAIR] WITH CHECK CHECK CONSTRAINT [FK_FilterItemPair_to_SessionConfig];

ALTER TABLE [dbo].[LATENCY_POLL] WITH CHECK CHECK CONSTRAINT [LATENCY_POLL.FK_MigrationSource.fkey];

ALTER TABLE [dbo].[LINK_ARTIFACT_LINK] WITH CHECK CHECK CONSTRAINT [FK_ArtifactLink_to_LinkType];

ALTER TABLE [dbo].[LINK_LINK_CHANGE_ACTIONS] WITH CHECK CHECK CONSTRAINT [FK_LinkChangeAction_to_ArtifactLink];

ALTER TABLE [dbo].[LINK_LINK_CHANGE_ACTIONS] WITH CHECK CHECK CONSTRAINT [FK_LinkChangeAction_to_LinkChangeGroup];

ALTER TABLE [dbo].[LINK_LINK_TYPE] WITH CHECK CHECK CONSTRAINT [FK_LinkType_to_ArtifactTypeSource];

ALTER TABLE [dbo].[LINK_LINK_TYPE] WITH CHECK CHECK CONSTRAINT [FK_LinkType_to_ArtifactTypeTarget];

ALTER TABLE [dbo].[MIGRATION_SOURCE_CONFIGS] WITH CHECK CHECK CONSTRAINT [FK_MigrationSourceConfigs];

ALTER TABLE [dbo].[MIGRATION_SOURCES] WITH CHECK CHECK CONSTRAINT [FK_MigrationSources1];

ALTER TABLE [dbo].[RELATED_ARTIFACTS_RECORDS] WITH CHECK CHECK CONSTRAINT [FK_RelatedRecord_to_MigrationSource];

ALTER TABLE [dbo].[RUNTIME_CHANGE_ACTION] WITH CHECK CHECK CONSTRAINT [FK_RT_ChangeActions];

ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] WITH CHECK CHECK CONSTRAINT [FK_RT_ChangeGroups_to_SessionRun];

ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] WITH CHECK CHECK CONSTRAINT [FK_RT_ChangeGroups1];

ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] WITH CHECK CHECK CONSTRAINT [FK_ConvHistory_to_ChangeGroup];

ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] WITH CHECK CHECK CONSTRAINT [FK_ConvHistory_to_MigrationSource];

ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] WITH CHECK CHECK CONSTRAINT [FK_ConvHistory_to_SessionRun];

ALTER TABLE [dbo].[RUNTIME_GENERAL_PERFORMANCE_DATA] WITH CHECK CHECK CONSTRAINT [FK_PerfData_To_SessionGroupRun];

ALTER TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS] WITH CHECK CHECK CONSTRAINT [FK_RT_ItemRevPairs];

ALTER TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS] WITH CHECK CHECK CONSTRAINT [FK_RT_ItemRevPairs1];

ALTER TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS] WITH CHECK CHECK CONSTRAINT [FK_RT_ItemRevPairs2];

ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS] WITH CHECK CHECK CONSTRAINT [FK_RT_MigrationItems];

ALTER TABLE [dbo].[RUNTIME_ORCHESTRATION_COMMAND] WITH CHECK CHECK CONSTRAINT [FK_OrchCmd_to_SessionGroup];

ALTER TABLE [dbo].[RUNTIME_SESSION_GROUP_RUNS] WITH CHECK CHECK CONSTRAINT [FK_RT_SessionGroupRuns1];

ALTER TABLE [dbo].[RUNTIME_SESSION_RUNS] WITH CHECK CHECK CONSTRAINT [FK_RT_SessionRuns1];

ALTER TABLE [dbo].[RUNTIME_SESSION_RUNS] WITH CHECK CHECK CONSTRAINT [FK_RT_SessionRuns2];

ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH CHECK CHECK CONSTRAINT [FK_RT_Sessions1];

ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH CHECK CHECK CONSTRAINT [FK_RT_Sessions2];

ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH CHECK CHECK CONSTRAINT [FK_RT_Sessions3];

ALTER TABLE [dbo].[SERVER_DIFF_RESULT_DETAIL] WITH CHECK CHECK CONSTRAINT [FK_SERVER_DIFF_RESULT.FK_SERVER_DIFF_RESULT.fkey];

ALTER TABLE [dbo].[SESSION_CONFIGURATIONS] WITH CHECK CHECK CONSTRAINT [FK_SessionConfiguration5];

ALTER TABLE [dbo].[SESSION_CONFIGURATIONS] WITH CHECK CHECK CONSTRAINT [FK_SessionConfigurations2];

ALTER TABLE [dbo].[SESSION_CONFIGURATIONS] WITH CHECK CHECK CONSTRAINT [FK_SessionConfigurations3];

ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS] WITH CHECK CHECK CONSTRAINT [FK_SessionGroupConfigs1];

ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS] WITH CHECK CHECK CONSTRAINT [FK_SessionGroupConfigs3];

ALTER TABLE [dbo].[STORED_CREDENTIALS] WITH CHECK CHECK CONSTRAINT [FK_StoredCredentials];

ALTER TABLE [dbo].[VALUE_MAP_COLLECTION] WITH CHECK CHECK CONSTRAINT [FK_ValueMapCollection1];

ALTER TABLE [dbo].[VALUE_MAP_COLLECTION] WITH CHECK CHECK CONSTRAINT [FK_ValueMapCollection2];

ALTER TABLE [dbo].[WI_TYPE_MAP_COLLECTION] WITH CHECK CHECK CONSTRAINT [FK_WITypeMapCollection];

ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH CHECK CHECK CONSTRAINT [chkSingleUsageOfMigrationSourceInSessions];


GO
