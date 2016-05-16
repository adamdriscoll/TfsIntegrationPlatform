--Stage One DB cleanup: remove the completed migration instruction ChangeActions

CREATE PROCEDURE [dbo].[prc_Stage1DBCleanup]
AS
	DECLARE @DeletionChangeActionId TABLE(Id BIGINT)

	-- RETRIEVE FIRST BATCH
	INSERT INTO @DeletionChangeActionId(Id)
	SELECT TOP 10000 a.ChangeActionId
	FROM dbo.RUNTIME_CHANGE_ACTION AS a
	INNER JOIN dbo.RUNTIME_CHANGE_GROUPS AS g
	   ON g.Id = a.ChangeGroupId
	WHERE (g.Status = 6 AND g.ContainsBackloggedAction = 0)


	-- SKIP DELETION IF THERE ARE NOT *MANY* JUNK IN DB
	WHILE (SELECT COUNT(*) FROM @DeletionChangeActionId) = 10000
	BEGIN
		PRINT CONVERT(VARCHAR(12),GETDATE(),114) + N' - Deleting 10000 RUNTIME_CHANGE_ACTION rows'
		DELETE FROM dbo.RUNTIME_CHANGE_ACTION
		WHERE ChangeActionId IN (SELECT Id FROM @DeletionChangeActionId)
		
		-- RETRIEVE NEXT BATCH
		DELETE FROM @DeletionChangeActionId	
		
		INSERT INTO @DeletionChangeActionId(Id)
		SELECT TOP 10000 a.ChangeActionId
		FROM dbo.RUNTIME_CHANGE_ACTION AS a
		INNER JOIN dbo.RUNTIME_CHANGE_GROUPS AS g
		   ON g.Id = a.ChangeGroupId
		WHERE (g.Status = 6 AND g.ContainsBackloggedAction = 0)	
	END
RETURN 0;