USE [Tfs_IntegrationPlatform]

BEGIN TRANSACTION UpgradeTipDB;
BEGIN TRY

	PRINT N'Dropping FriendlyName...';


	EXECUTE sp_dropextendedproperty @name = N'FriendlyName';



	PRINT N'Dropping ReferenceName...';



	EXECUTE sp_dropextendedproperty @name = N'ReferenceName';



	PRINT N'Dropping dbo.RELATED_ARTIFACTS_RECORDS.MigrationSource_ItemId_Relationship_RelatedArtifactId...';



	DROP INDEX [MigrationSource_ItemId_Relationship_RelatedArtifactId]
		ON [dbo].[RELATED_ARTIFACTS_RECORDS];



	PRINT N'Dropping dbo.RELATED_ARTIFACTS_RECORDS.MigrationSource_ItemId_ExistOnServer...';



	DROP INDEX [MigrationSource_ItemId_ExistOnServer]
		ON [dbo].[RELATED_ARTIFACTS_RECORDS];



	PRINT N'Altering dbo.RELATED_ARTIFACTS_RECORDS...';



	ALTER TABLE [dbo].[RELATED_ARTIFACTS_RECORDS] ALTER COLUMN [ItemId] NVARCHAR (4000) NOT NULL;

	ALTER TABLE [dbo].[RELATED_ARTIFACTS_RECORDS] ALTER COLUMN [RelatedArtifactId] NVARCHAR (4000) NOT NULL;

	ALTER TABLE [dbo].[RELATED_ARTIFACTS_RECORDS] ALTER COLUMN [Relationship] NVARCHAR (1000) NOT NULL;



	PRINT N'Altering dbo.SESSION_GROUP_CONFIGS...';



	ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS]
		ADD [ErrorManagementConfig] XML NULL;



	PRINT N'Creating dbo.RELATED_ARTIFACTS_RECORDS.MigrationSource_ItemId...';



	CREATE NONCLUSTERED INDEX [MigrationSource_ItemId]
		ON [dbo].[RELATED_ARTIFACTS_RECORDS]([MigrationSourceId] ASC)
		INCLUDE([ItemId]) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);



	PRINT N'Creating dbo.RELATED_ARTIFACTS_RECORDS.MigrationSource_RelatedArtifactId...';



	CREATE NONCLUSTERED INDEX [MigrationSource_RelatedArtifactId]
		ON [dbo].[RELATED_ARTIFACTS_RECORDS]([MigrationSourceId] ASC)
		INCLUDE([RelatedArtifactId]) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);



	PRINT N'Creating FriendlyName...';



	EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v1.9';



	PRINT N'Creating ReferenceName...';


	EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = '3385CC83-67AE-4ca3-A0F4-88596EA4FF05';


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
