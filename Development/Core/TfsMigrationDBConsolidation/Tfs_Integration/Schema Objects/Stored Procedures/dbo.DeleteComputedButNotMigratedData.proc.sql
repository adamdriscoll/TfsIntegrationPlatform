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