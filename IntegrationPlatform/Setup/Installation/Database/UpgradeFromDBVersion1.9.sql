USE [Tfs_IntegrationPlatform]
	
BEGIN TRANSACTION UpgradeTipDB;
BEGIN TRY


	PRINT N'Dropping FriendlyName...';



	EXECUTE sp_dropextendedproperty @name = N'FriendlyName';



	PRINT N'Dropping ReferenceName...';



	EXECUTE sp_dropextendedproperty @name = N'ReferenceName';



	PRINT N'Altering dbo.LINK_ARTIFACT_LINK...';



	ALTER TABLE [dbo].[LINK_ARTIFACT_LINK]
		ADD [IsLocked] BIT NULL;



	PRINT N'Creating FriendlyName...';



	EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v2.0';



	PRINT N'Creating ReferenceName...';



	EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = 'E0D84458-23B4-48C1-B7F5-8A2799FD82F4';





	-- Project upgrade has moved this code to 'Upgraded.extendedproperties.sql'.

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





