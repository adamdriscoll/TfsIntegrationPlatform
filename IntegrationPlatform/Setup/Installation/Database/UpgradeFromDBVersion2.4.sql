USE [Tfs_IntegrationPlatform]
 
PRINT N'Dropping [FriendlyName]...';



EXECUTE sp_dropextendedproperty @name = N'FriendlyName';



PRINT N'Dropping [ReferenceName]...';



EXECUTE sp_dropextendedproperty @name = N'ReferenceName';



PRINT N'Dropping FK_RT_ChangeActions...';



ALTER TABLE [dbo].[RUNTIME_CHANGE_ACTION] DROP CONSTRAINT [FK_RT_ChangeActions];



PRINT N'Dropping FK_RT_ChangeGroups_to_SessionRun...';



ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] DROP CONSTRAINT [FK_RT_ChangeGroups_to_SessionRun];



PRINT N'Dropping FK_RT_ChangeGroups1...';



ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] DROP CONSTRAINT [FK_RT_ChangeGroups1];



PRINT N'Dropping FK_ConvHistory_to_ChangeGroup...';



ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] DROP CONSTRAINT [FK_ConvHistory_to_ChangeGroup];



PRINT N'Altering [dbo].[MIGRATION_SOURCES]...';



ALTER TABLE [dbo].[MIGRATION_SOURCES]
    ADD [NativeId] NVARCHAR (400) NULL;



PRINT N'Starting rebuilding table [dbo].[RUNTIME_CHANGE_GROUPS]...';



SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

BEGIN TRANSACTION;

CREATE TABLE [dbo].[tmp_ms_xx_RUNTIME_CHANGE_GROUPS] (
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

ALTER TABLE [dbo].[tmp_ms_xx_RUNTIME_CHANGE_GROUPS]
    ADD CONSTRAINT [tmp_ms_xx_clusteredindex_PK_RT_ChangeGroups] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);

IF EXISTS (SELECT TOP 1 1
           FROM   [dbo].[RUNTIME_CHANGE_GROUPS])
    BEGIN
        SET IDENTITY_INSERT [dbo].[tmp_ms_xx_RUNTIME_CHANGE_GROUPS] ON;
        INSERT INTO [dbo].[tmp_ms_xx_RUNTIME_CHANGE_GROUPS] ([Id], [Name], [ExecutionOrder], [SourceMigrationSourceId], [Owner], [Comment], [StartTime], [FinishTime], [SessionUniqueId], [SourceUniqueId], [Status], [SessionRunId], [ContainsBackloggedAction], [ReflectedChangeGroupId], [UsePagedActions])
        SELECT   [Id],
                 [Name],
                 [ExecutionOrder],
                 [SourceMigrationSourceId],
                 [Owner],
                 [Comment],
                 [StartTime],
                 [FinishTime],
                 [SessionUniqueId],
                 [SourceUniqueId],
                 [Status],
                 [SessionRunId],
                 [ContainsBackloggedAction],
                 [ReflectedChangeGroupId],
                 [UsePagedActions]
        FROM     [dbo].[RUNTIME_CHANGE_GROUPS]
        ORDER BY [Id] ASC;
        SET IDENTITY_INSERT [dbo].[tmp_ms_xx_RUNTIME_CHANGE_GROUPS] OFF;
    END

DROP TABLE [dbo].[RUNTIME_CHANGE_GROUPS];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_RUNTIME_CHANGE_GROUPS]', N'RUNTIME_CHANGE_GROUPS';

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_clusteredindex_PK_RT_ChangeGroups]', N'PK_RT_ChangeGroups', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;



PRINT N'Creating UK_RT_ChangeGroups...';



ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS]
    ADD CONSTRAINT [UK_RT_ChangeGroups] UNIQUE NONCLUSTERED ([ExecutionOrder] ASC, [SourceMigrationSourceId] ASC, [Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);



PRINT N'Creating [dbo].[RUNTIME_CHANGE_GROUPS].[SessionUID_SourceUID_Status]...';



CREATE NONCLUSTERED INDEX [SessionUID_SourceUID_Status]
    ON [dbo].[RUNTIME_CHANGE_GROUPS]([SessionUniqueId] ASC, [SourceUniqueId] ASC, [Status] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);



PRINT N'Creating [dbo].[SERVER_DIFF_RESULT]...';



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



PRINT N'Creating [dbo].[SERVER_DIFF_RESULT_DETAIL]...';



CREATE TABLE [dbo].[SERVER_DIFF_RESULT_DETAIL] (
    [Id]                 BIGINT         IDENTITY (1, 1) NOT NULL,
    [ServerDiffResultId] BIGINT         NOT NULL,
    [DiffDescription]    NVARCHAR (MAX) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);



PRINT N'Creating FK_RT_ChangeActions...';



ALTER TABLE [dbo].[RUNTIME_CHANGE_ACTION] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ChangeActions] FOREIGN KEY ([ChangeGroupId]) REFERENCES [dbo].[RUNTIME_CHANGE_GROUPS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



PRINT N'Creating FK_RT_ChangeGroups_to_SessionRun...';



ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ChangeGroups_to_SessionRun] FOREIGN KEY ([SessionRunId]) REFERENCES [dbo].[RUNTIME_SESSION_RUNS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



PRINT N'Creating FK_RT_ChangeGroups1...';



ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] WITH NOCHECK
    ADD CONSTRAINT [FK_RT_ChangeGroups1] FOREIGN KEY ([SourceMigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



PRINT N'Creating FK_ConvHistory_to_ChangeGroup...';



ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] WITH NOCHECK
    ADD CONSTRAINT [FK_ConvHistory_to_ChangeGroup] FOREIGN KEY ([SourceChangeGroupId]) REFERENCES [dbo].[RUNTIME_CHANGE_GROUPS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



PRINT N'Creating FK_SERVER_DIFF_RESULT.FK_SERVER_DIFF_RESULT.fkey...';



ALTER TABLE [dbo].[SERVER_DIFF_RESULT_DETAIL] WITH NOCHECK
    ADD CONSTRAINT [FK_SERVER_DIFF_RESULT.FK_SERVER_DIFF_RESULT.fkey] FOREIGN KEY ([ServerDiffResultId]) REFERENCES [dbo].[SERVER_DIFF_RESULT] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



PRINT N'Creating [dbo].[SessionGroupDetailsView]...';


GO
CREATE VIEW [dbo].[SessionGroupDetailsView]
	AS 
  SELECT sg.[GroupUniqueId] AS SessionGroupUniqueId
      ,sg.[FriendlyName] AS SessionGroupFriendlyName
      ,s.SessionUniqueId AS SessionUniqueId
      ,sc.FriendlyName AS SessionFriendlyName
      ,sc.Type As SessionType
	  ,ms.Id AS MigrationSourceId
      ,ms.UniqueId AS MigrationSourceUniqueId
      ,ms.FriendlyName As MigrationSourceFriendlyName
  FROM [dbo].[SESSION_GROUPS] AS sg
  INNER JOIN [dbo].[SESSION_GROUP_CONFIGS] AS sgc ON sgc.SessionGroupId = sg.Id
  INNER JOIN [dbo].[RUNTIME_SESSIONS] AS s ON sg.Id = s.SessionGroupId
  INNER JOIN [dbo].[SESSION_CONFIGURATIONS] AS sc ON sc.SessionGroupConfigId = sgc.Id
  INNER JOIN [dbo].[MIGRATION_SOURCES] AS ms ON (ms.Id = s.LeftSourceId OR ms.Id = s.RightSourceId)
  GROUP BY sg.[GroupUniqueId]
      ,sg.[FriendlyName]
      ,s.SessionUniqueId
      ,sc.FriendlyName
      ,sc.Type
	  ,ms.Id
      ,ms.UniqueId
      ,ms.FriendlyName

GO
PRINT N'Creating [FriendlyName]...';



EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Integration Platform Database v2.5';



PRINT N'Creating [ReferenceName]...';



EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = 'BC951692-F5EC-4FB9-BDBF-C859B5714372';



-- Project upgrade has moved this code to 'Upgraded.extendedproperties.sql'.


PRINT N'Checking existing data against newly created constraints';



ALTER TABLE [dbo].[RUNTIME_CHANGE_ACTION] WITH CHECK CHECK CONSTRAINT [FK_RT_ChangeActions];

ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] WITH CHECK CHECK CONSTRAINT [FK_RT_ChangeGroups_to_SessionRun];

ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS] WITH CHECK CHECK CONSTRAINT [FK_RT_ChangeGroups1];

ALTER TABLE [dbo].[RUNTIME_CONVERSION_HISTORY] WITH CHECK CHECK CONSTRAINT [FK_ConvHistory_to_ChangeGroup];

ALTER TABLE [dbo].[SERVER_DIFF_RESULT_DETAIL] WITH CHECK CHECK CONSTRAINT [FK_SERVER_DIFF_RESULT.FK_SERVER_DIFF_RESULT.fkey];

