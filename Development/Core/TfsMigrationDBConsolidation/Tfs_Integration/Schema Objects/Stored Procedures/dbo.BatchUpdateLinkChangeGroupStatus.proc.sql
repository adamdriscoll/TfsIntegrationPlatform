CREATE PROCEDURE [dbo].[BatchUpdateLinkChangeGroupStatus]
	@SessionGroupId uniqueidentifier, 
	@SessionId uniqueidentifier,
	@SourceId uniqueidentifier,
	@ContainsConflictedAction bit,
	@CurrentStatus int,
	@NewStatus int
AS
	UPDATE [dbo].[LINK_LINK_CHANGE_GROUPS]
	set Status = @NewStatus
	where Status = @CurrentStatus
	  and SessionGroupUniqueId = @SessionGroupId
	  and SessionUniqueId = @SessionId
	  and SourceId = @SourceId
	  and ContainsConflictedAction = @ContainsConflictedAction;
	
	select *
	from LINK_LINK_CHANGE_GROUPS
	where Status = @NewStatus
	  and SessionGroupUniqueId = @SessionGroupId
	  and SessionUniqueId = @SessionId
	  and SourceId = @SourceId
	  and ContainsConflictedAction = @ContainsConflictedAction;