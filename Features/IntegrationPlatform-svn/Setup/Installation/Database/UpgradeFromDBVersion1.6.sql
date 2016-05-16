USE [Tfs_IntegrationPlatform]

BEGIN TRANSACTION UpgradeTipDB;
BEGIN TRY

	PRINT N'Dropping FriendlyName...';



	EXECUTE sp_dropextendedproperty @name = N'FriendlyName';



	PRINT N'Dropping ReferenceName...';



	EXECUTE sp_dropextendedproperty @name = N'ReferenceName';



	PRINT N'Dropping dbo.FK_FilterItemPair_to_SessionConfig...';



	ALTER TABLE [dbo].[FILTER_ITEM_PAIR] DROP CONSTRAINT [FK_FilterItemPair_to_SessionConfig];



	PRINT N'Starting rebuilding table dbo.FILTER_ITEM_PAIR...';



	SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

	SET XACT_ABORT ON;



	BEGIN TRANSACTION;

	CREATE TABLE [dbo].[tmp_ms_xx_FILTER_ITEM_PAIR] (
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

	ALTER TABLE [dbo].[tmp_ms_xx_FILTER_ITEM_PAIR]
		ADD CONSTRAINT [tmp_ms_xx_clusteredindex_PK_FilterItemPair] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);

	IF EXISTS (SELECT TOP 1 1
			   FROM   [dbo].[FILTER_ITEM_PAIR])
		BEGIN
			SET IDENTITY_INSERT [dbo].[tmp_ms_xx_FILTER_ITEM_PAIR] ON;
			INSERT INTO [dbo].[tmp_ms_xx_FILTER_ITEM_PAIR] ([Id], [Filter1MigrationSourceReferenceName], [Filter1], [Filter1SnapshotPoint], [Filter1MergeScope], [Filter2MigrationSourceReferenceName], [Filter2], [Filter2SnapshotPoint], [Filter2MergeScope], [Neglect], [SessionConfigId])
			SELECT   [Id],
					 [Filter1MigrationSourceReferenceName],
					 [Filter1],
					 [Filter1SnapshotPoint],
					 [Filter1MergeScope],
					 [Filter2MigrationSourceReferenceName],
					 [Filter2],
					 [Filter2SnapshotPoint],
					 [Filter2MergeScope],
					 [Neglect],
					 [SessionConfigId]
			FROM     [dbo].[FILTER_ITEM_PAIR]
			ORDER BY [Id] ASC;
			SET IDENTITY_INSERT [dbo].[tmp_ms_xx_FILTER_ITEM_PAIR] OFF;
		END

	DROP TABLE [dbo].[FILTER_ITEM_PAIR];

	EXECUTE sp_rename N'[dbo].[tmp_ms_xx_FILTER_ITEM_PAIR]', N'FILTER_ITEM_PAIR';

	EXECUTE sp_rename N'[dbo].[tmp_ms_xx_clusteredindex_PK_FilterItemPair]', N'PK_FilterItemPair', N'OBJECT';

	COMMIT TRANSACTION;



	PRINT N'Creating dbo.FK_FilterItemPair_to_SessionConfig...';



	ALTER TABLE [dbo].[FILTER_ITEM_PAIR]
		ADD CONSTRAINT [FK_FilterItemPair_to_SessionConfig] FOREIGN KEY ([SessionConfigId]) REFERENCES [dbo].[SESSION_CONFIGURATIONS] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



	PRINT N'Creating FriendlyName...';



	EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v1.7';



	PRINT N'Creating ReferenceName...';



	EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = 'E5CFE584-A193-48F5-B015-EF13593B60CE';

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
