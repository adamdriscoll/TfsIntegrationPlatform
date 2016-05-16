USE [Tfs_IntegrationPlatform]

BEGIN TRANSACTION UpgradeTipDB;
BEGIN TRY

	PRINT N'Dropping FriendlyName...';



	EXECUTE sp_dropextendedproperty @name = N'FriendlyName';



	PRINT N'Dropping ReferenceName...';



	EXECUTE sp_dropextendedproperty @name = N'ReferenceName';



	PRINT N'Dropping dbo.FK_ContentResv1...';



	ALTER TABLE [dbo].[CONFLICT_CONTENT_RESV] DROP CONSTRAINT [FK_ContentResv1];



	PRINT N'Dropping dbo.FK_Conflicts_to_ResolveRule...';


	ALTER TABLE [dbo].[CONFLICT_CONFLICTS] DROP CONSTRAINT [FK_Conflicts_to_ResolveRule];



	PRINT N'Dropping dbo.FK_Conflicts_to_MigrationSource...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS] DROP CONSTRAINT [FK_Conflicts_to_MigrationSource];



	PRINT N'Dropping dbo.FK_Conflicts_to_LinkChangeGroup...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS] DROP CONSTRAINT [FK_Conflicts_to_LinkChangeGroup];



	PRINT N'Dropping dbo.FK_Conflicts_to_LinkChangeAction...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS] DROP CONSTRAINT [FK_Conflicts_to_LinkChangeAction];



	PRINT N'Dropping dbo.FK_Conflicts_to_ConflictType...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS] DROP CONSTRAINT [FK_Conflicts_to_ConflictType];



	PRINT N'Dropping dbo.FK_Conflicts_to_ConflictCollection...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS] DROP CONSTRAINT [FK_Conflicts_to_ConflictCollection];



	PRINT N'Dropping dbo.FK_Conflicts_to_ChangeAction...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS] DROP CONSTRAINT [FK_Conflicts_to_ChangeAction];



	PRINT N'Starting rebuilding table dbo.CONFLICT_CONFLICTS...';



	SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

	SET XACT_ABORT ON;



	BEGIN TRANSACTION;

	CREATE TABLE [dbo].[tmp_ms_xx_CONFLICT_CONFLICTS] (
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

	ALTER TABLE [dbo].[tmp_ms_xx_CONFLICT_CONFLICTS]
		ADD CONSTRAINT [tmp_ms_xx_clusteredindex_PK_Conflicts] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);

	IF EXISTS (SELECT TOP 1 1
			   FROM   [dbo].[CONFLICT_CONFLICTS])
		BEGIN
			SET IDENTITY_INSERT [dbo].[tmp_ms_xx_CONFLICT_CONFLICTS] ON;
			INSERT INTO [dbo].[tmp_ms_xx_CONFLICT_CONFLICTS] ([Id], [ConflictListId], [ConflictTypeId], [ConflictDetails], [ConflictedChangeActionId], [ChangeGroupId], [ConflictedLinkChangeActionId], [ConflictedLinkChangeGroupId], [ScopeId], [SourceMigrationSourceId], [ScopeHint], [Status], [ResolvedByRuleId], [CreationTime])
			SELECT   [Id],
					 [ConflictListId],
					 [ConflictTypeId],
					 [ConflictDetails],
					 [ConflictedChangeActionId],
					 [ChangeGroupId],
					 [ConflictedLinkChangeActionId],
					 [ConflictedLinkChangeGroupId],
					 [ScopeId],
					 [SourceMigrationSourceId],
					 [ScopeHint],
					 [Status],
					 [ResolvedByRuleId],
					 [CreationTime]
			FROM     [dbo].[CONFLICT_CONFLICTS]
			ORDER BY [Id] ASC;
			SET IDENTITY_INSERT [dbo].[tmp_ms_xx_CONFLICT_CONFLICTS] OFF;
		END

	DROP TABLE [dbo].[CONFLICT_CONFLICTS];

	EXECUTE sp_rename N'[dbo].[tmp_ms_xx_CONFLICT_CONFLICTS]', N'CONFLICT_CONFLICTS';

	EXECUTE sp_rename N'[dbo].[tmp_ms_xx_clusteredindex_PK_Conflicts]', N'PK_Conflicts', N'OBJECT';

	COMMIT TRANSACTION;



	PRINT N'Creating dbo.FK_ContentResv1...';



	ALTER TABLE [dbo].[CONFLICT_CONTENT_RESV]
		ADD CONSTRAINT [FK_ContentResv1] FOREIGN KEY ([ConflictId]) REFERENCES [dbo].[CONFLICT_CONFLICTS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



	PRINT N'Creating dbo.FK_Conflicts_to_ResolveRule...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
		ADD CONSTRAINT [FK_Conflicts_to_ResolveRule] FOREIGN KEY ([ResolvedByRuleId]) REFERENCES [dbo].[CONFLICT_RESOLUTION_RULES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



	PRINT N'Creating dbo.FK_Conflicts_to_MigrationSource...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
		ADD CONSTRAINT [FK_Conflicts_to_MigrationSource] FOREIGN KEY ([SourceMigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



	PRINT N'Creating dbo.FK_Conflicts_to_LinkChangeGroup...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
		ADD CONSTRAINT [FK_Conflicts_to_LinkChangeGroup] FOREIGN KEY ([ConflictedLinkChangeGroupId]) REFERENCES [dbo].[LINK_LINK_CHANGE_GROUPS] ([Id]) ON DELETE SET NULL ON UPDATE NO ACTION;



	PRINT N'Creating dbo.FK_Conflicts_to_LinkChangeAction...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
		ADD CONSTRAINT [FK_Conflicts_to_LinkChangeAction] FOREIGN KEY ([ConflictedLinkChangeActionId]) REFERENCES [dbo].[LINK_LINK_CHANGE_ACTIONS] ([Id]) ON DELETE SET NULL ON UPDATE NO ACTION;



	PRINT N'Creating dbo.FK_Conflicts_to_ConflictType...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
		ADD CONSTRAINT [FK_Conflicts_to_ConflictType] FOREIGN KEY ([ConflictTypeId]) REFERENCES [dbo].[CONFLICT_CONFLICT_TYPES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



	PRINT N'Creating dbo.FK_Conflicts_to_ConflictCollection...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
		ADD CONSTRAINT [FK_Conflicts_to_ConflictCollection] FOREIGN KEY ([ConflictListId]) REFERENCES [dbo].[RUNTIME_CONFLICT_COLLECTIONS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



	PRINT N'Creating dbo.FK_Conflicts_to_ChangeAction...';



	ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
		ADD CONSTRAINT [FK_Conflicts_to_ChangeAction] FOREIGN KEY ([ChangeGroupId], [ConflictedChangeActionId]) REFERENCES [dbo].[RUNTIME_CHANGE_ACTION] ([ChangeGroupId], [ChangeActionId]) ON DELETE SET NULL ON UPDATE NO ACTION;



	PRINT N'Creating FriendlyName...';



	EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v1.6';



	PRINT N'Creating ReferenceName...';



	EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = '94F47334-ACD5-42E9-A50A-4EF2BFFF63A0';

	PRINT N'Committing...';
	COMMIT TRANSACTION UpgradeTipDB;
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
        ROLLBACK TRANSACTION UpgradeTipDB;
        RAISERROR (@ErrorMessage,@ErrorSeverity, @ErrorState)
END CATCH;