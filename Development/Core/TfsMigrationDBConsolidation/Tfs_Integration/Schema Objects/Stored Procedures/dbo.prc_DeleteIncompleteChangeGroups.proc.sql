CREATE PROCEDURE [dbo].[prc_DeleteIncompleteChangeGroups]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId uniqueidentifier
AS
	delete from RUNTIME_CHANGE_ACTION
	where ChangeGroupId in 
		(select Id 
		from RUNTIME_CHANGE_GROUPS
		where SessionUniqueId = @SessionUniqueId 
		and SourceUniqueId = @SourceUniqueId
		and Status = 20); -- 20: ChangeCreationInProgress
	
	delete from RUNTIME_CHANGE_GROUPS
	where SessionUniqueId = @SessionUniqueId 
	and SourceUniqueId = @SourceUniqueId
	and Status = 20; -- 20: ChangeCreationInProgress
	
	SELECT TOP 1 *
	FROM RUNTIME_CHANGE_GROUPS
	where SessionUniqueId = @SessionUniqueId 
	and SourceUniqueId = @SourceUniqueId 

	
RETURN 0;