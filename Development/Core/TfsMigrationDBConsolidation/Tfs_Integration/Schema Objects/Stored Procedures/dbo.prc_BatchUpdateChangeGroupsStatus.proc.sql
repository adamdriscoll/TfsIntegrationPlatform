CREATE PROCEDURE [dbo].[prc_BatchUpdateChangeGroupsStatus]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId  uniqueidentifier,
	@CurrStatus int,
	@NewStatus int
AS
	UPDATE [dbo].[RUNTIME_CHANGE_GROUPS] 
	SET Status = @NewStatus
	WHERE SessionUniqueId = @SessionUniqueId
	AND SourceUniqueId = @SourceUniqueId
	AND Status = @CurrStatus
SELECT * 
	FROM [dbo].[RUNTIME_CHANGE_GROUPS] 
	WHERE SessionUniqueId = @SessionUniqueId
	AND SourceUniqueId = @SourceUniqueId
	AND Status = @CurrStatus