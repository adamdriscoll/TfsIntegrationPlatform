CREATE PROCEDURE [dbo].[prc_PromoteChangeGroups]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId  uniqueidentifier,
	@CurrentStatus int,
	@NewStatus int
AS
UPDATE [dbo].[RUNTIME_CHANGE_GROUPS]
WITH (UPDLOCK)
SET Status=@NewStatus -- DeltaPending
WHERE SessionUniqueId = @SessionUniqueId
	AND SourceUniqueId = @SourceUniqueId
	AND Status = @CurrentStatus -- Delta
	AND ContainsBackloggedAction = 0
	
SELECT * FROM [dbo].[RUNTIME_CHANGE_GROUPS] 
WHERE SessionUniqueId = @SessionUniqueId
	AND SourceUniqueId = @SourceUniqueId
	AND Status = @NewStatus

RETURN 0;