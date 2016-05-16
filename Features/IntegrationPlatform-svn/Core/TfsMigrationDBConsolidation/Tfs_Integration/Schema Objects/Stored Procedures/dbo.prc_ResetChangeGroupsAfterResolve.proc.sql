CREATE PROCEDURE [dbo].[prc_ResetChangeGroupsAfterResolve]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId uniqueidentifier,
	@DeltaTableExecutionOrder bigint
AS
-- Reset all later delta table entries back to deltapending	
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 1 -- DeltaPending
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND ExecutionOrder > @DeltaTableExecutionOrder
	
-- Reset all unprocessed migration instructions
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 10 -- Obsolete
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND Status = 4 OR Status = 5 OR Status = 9 -- Pending or InProgress or PendingConflictDetection
	
	SELECT * FROM RUNTIME_CHANGE_GROUPS
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND ExecutionOrder = @DeltaTableExecutionOrder

RETURN 0;