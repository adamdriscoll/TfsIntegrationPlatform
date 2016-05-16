Use [Tfs_IntegrationPlatform]

GO
PRINT N'Dropping [FriendlyName]...';

GO
EXECUTE sp_dropextendedproperty @name = N'FriendlyName';

GO
PRINT N'Dropping [ReferenceName]...';

GO
EXECUTE sp_dropextendedproperty @name = N'ReferenceName';



PRINT N'Creating [dbo].[FORCE_SYNC_ITEMS]...';
GO

CREATE TABLE [dbo].[FORCE_SYNC_ITEMS] (
    [Id]                BIGINT         IDENTITY (1, 1) NOT NULL,
    [SessionId]         INT            NOT NULL,
    [MigrationSourceId] INT            NOT NULL,
    [ItemId]            NVARCHAR (300) NOT NULL,
    [Status]            INT            NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);
GO

PRINT N'Creating FORCE_SYNC_ITEMS.FK_MigrationSource.fkey...';
GO

ALTER TABLE [dbo].[FORCE_SYNC_ITEMS] WITH NOCHECK
    ADD CONSTRAINT [FORCE_SYNC_ITEMS.FK_MigrationSource.fkey] FOREIGN KEY ([MigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

PRINT N'Altering [dbo].[FORCE_SYNC_ITEMS]...';
GO

ALTER TABLE [dbo].[FORCE_SYNC_ITEMS] WITH CHECK CHECK CONSTRAINT [FORCE_SYNC_ITEMS.FK_MigrationSource.fkey];
GO

PRINT N'Altering [dbo].[LINK_LINK_CHANGE_GROUPS]...';
GO

ALTER TABLE [dbo].[LINK_LINK_CHANGE_GROUPS]
    ADD [IsForcedSync] BIT NULL;
GO


PRINT N'Altering [dbo].[RUNTIME_CHANGE_GROUPS]...';
GO

ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS]
    ADD [IsForcedSync] BIT NULL;
GO

PRINT N'Creating [dbo].[CONFLICT_CONFLICTS].[CONFLICT_CONFLICTS_SourceMigrationSourceId_Status]...';
GO

CREATE NONCLUSTERED INDEX [CONFLICT_CONFLICTS_SourceMigrationSourceId_Status]
    ON [dbo].[CONFLICT_CONFLICTS]([SourceMigrationSourceId] ASC, [Status] ASC)
    INCLUDE([ConflictedChangeActionId], [ChangeGroupId]) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);
GO


PRINT N'Creating [FriendlyName]...';

GO
EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Integration Platform Database v2.12';

GO
PRINT N'Creating [ReferenceName]...';

GO
EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = '91294ED5-12E2-447D-A627-225E83D2854C';

