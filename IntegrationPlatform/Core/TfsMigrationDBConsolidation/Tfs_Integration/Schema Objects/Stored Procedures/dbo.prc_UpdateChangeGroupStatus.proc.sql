CREATE PROCEDURE [dbo].[prc_UpdateChangeGroupStatus]
	@Id int, 
	@NewStatus int
AS
	UPDATE [dbo].[RUNTIME_CHANGE_GROUPS] 
	SET Status = @NewStatus
	WHERE Id = @Id
SELECT * from [dbo].[RUNTIME_CHANGE_GROUPS] 
WHERE Id = @Id