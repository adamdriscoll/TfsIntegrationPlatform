CREATE FUNCTION [dbo].[MigrationSourceNotUsedInExistingSessions]
(
	@LeftSourceId int, 
	@RightSourceId int
)
RETURNS INT -- 0 if neither @LeftSourceId or @RightSourceId is used in an existing Session
            -- 1 in other cases
AS
BEGIN
	DECLARE @LeftSourceUsageCount INT
	DECLARE @RightSourceUsageCount INT
	DECLARE @RetVal INT
	
	SELECT @LeftSourceUsageCount = COUNT(*) 
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE RightSourceId = @LeftSourceId
	   OR LeftSourceId = @LeftSourceId
	
	-- the only usage should be the session row that this function is checking in CHECK CONSTRAINT
	IF @LeftSourceUsageCount > 1 
	BEGIN
		-- @LeftSourceId is already used by an existing session
		SET @RetVal = 1
	END
	ELSE
	BEGIN
		SELECT @RightSourceUsageCount = COUNT(*) 
		FROM [dbo].[RUNTIME_SESSIONS]
		WHERE RightSourceId = @RightSourceId
		   OR LeftSourceId = @RightSourceId
	
		-- the only usage should be the session row that this function is checking in CHECK CONSTRAINT	   
		IF @RightSourceUsageCount > 1
		BEGIN
			-- @RightSourceId is already used by an existing session
			SET @RetVal = 1
		END
		ELSE
		BEGIN
			SET @RetVal = 0
		END
	END
	
	RETURN @RetVal
END