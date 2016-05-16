USE [Tfs_IntegrationPlatform]

BEGIN TRANSACTION UpgradeTipDB;
BEGIN TRY


	PRINT N'Dropping FriendlyName...';



	EXECUTE sp_dropextendedproperty @name = N'FriendlyName';



	PRINT N'Dropping ReferenceName...';



	EXECUTE sp_dropextendedproperty @name = N'ReferenceName';



	PRINT N'Altering dbo.SESSION_GROUP_CONFIGS...';



	ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS] ALTER COLUMN [FriendlyName] NVARCHAR (300) NULL;



	PRINT N'Creating dbo.RELATED_ARTIFACTS_RECORDS...';



	CREATE TABLE [dbo].[RELATED_ARTIFACTS_RECORDS] (
		[Id]                         BIGINT         IDENTITY (1, 1) NOT NULL,
		[MigrationSourceId]          INT            NULL,
		[ItemId]                     NVARCHAR (300) NOT NULL,
		[Relationship]               NVARCHAR (300) NOT NULL,
		[RelatedArtifactId]          NVARCHAR (300) NOT NULL,
		[RelationshipExistsOnServer] BIT            NOT NULL,
		PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
	);



	PRINT N'Creating dbo.RELATED_ARTIFACTS_RECORDS.MigrationSource_ItemId_ExistOnServer...';



	CREATE NONCLUSTERED INDEX [MigrationSource_ItemId_ExistOnServer]
		ON [dbo].[RELATED_ARTIFACTS_RECORDS]([MigrationSourceId] ASC, [ItemId] ASC, [RelationshipExistsOnServer] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);



	PRINT N'Creating dbo.RELATED_ARTIFACTS_RECORDS.MigrationSource_ItemId_Relationship_RelatedArtifactId...';



	CREATE NONCLUSTERED INDEX [MigrationSource_ItemId_Relationship_RelatedArtifactId]
		ON [dbo].[RELATED_ARTIFACTS_RECORDS]([MigrationSourceId] ASC, [ItemId] ASC, [Relationship] ASC, [RelatedArtifactId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);



	PRINT N'Creating dbo.FK_RelatedRecord_to_MigrationSource...';



	ALTER TABLE [dbo].[RELATED_ARTIFACTS_RECORDS]
		ADD CONSTRAINT [FK_RelatedRecord_to_MigrationSource] FOREIGN KEY ([MigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;



	PRINT N'Creating FriendlyName...';



	EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v1.8';



	PRINT N'Creating ReferenceName...';



	EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = '3BE681EC-C301-4944-8190-720C73B8B76A';
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




