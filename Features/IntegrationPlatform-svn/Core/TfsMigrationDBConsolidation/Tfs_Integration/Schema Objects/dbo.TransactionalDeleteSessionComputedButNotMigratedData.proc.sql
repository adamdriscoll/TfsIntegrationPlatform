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