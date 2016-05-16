USE [Tfs_IntegrationPlatform]
 
BEGIN TRANSACTION UpgradeTipDB;
BEGIN TRY



PRINT N'Dropping [FriendlyName]...';



EXECUTE sp_dropextendedproperty @name = N'FriendlyName';



PRINT N'Dropping [ReferenceName]...';



EXECUTE sp_dropextendedproperty @name = N'ReferenceName';



PRINT N'Creating [dbo].[RUNTIME_CHANGE_GROUPS].[SessionUID_SourceUID_Status]...';



CREATE NONCLUSTERED INDEX [SessionUID_SourceUID_Status]
    ON [dbo].[RUNTIME_CHANGE_GROUPS]([SessionUniqueId] ASC, [SourceUniqueId] ASC, [Status] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0);



PRINT N'Creating [FriendlyName]...';



EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Integration Platform Database v2.4';



PRINT N'Creating [ReferenceName]...';



EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = '84DA2ED6-69C6-4B53-934B-EF0540412F9A';



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

