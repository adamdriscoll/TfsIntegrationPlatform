CREATE PROCEDURE [dbo].[UpdateMigrationSessionStatusToCompleted]
	@SessionId uniqueidentifier
AS
	DECLARE @SessionInternalId int;
	DECLARE @SessionGroupInternalId int;
	DECLARE @NumOfUncompletedSession int;
	
	SELECT @SessionInternalId = Id
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE SessionUniqueId = @SessionId;
	
	IF @SessionInternalId IS NULL
	BEGIN
		RETURN 0;
	END
		
	UPDATE [dbo].[RUNTIME_SESSIONS]
	SET State = 3
	WHERE Id = @SessionInternalId;
	
	SELECT @SessionGroupInternalId = SessionGroupId
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE Id = @SessionInternalId;
	
	SELECT @NumOfUncompletedSession = COUNT(*)
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE SessionGroupId = @SessionGroupInternalId
	  AND State <> 3;
	  
	IF @NumOfUncompletedSession = 0
	BEGIN
		UPDATE [dbo].[SESSION_GROUPS]
		SET State = 3
		WHERE Id = @SessionGroupInternalId;
	END
	
	SELECT *
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE Id = @SessionInternalId;
	
RETURN 0;