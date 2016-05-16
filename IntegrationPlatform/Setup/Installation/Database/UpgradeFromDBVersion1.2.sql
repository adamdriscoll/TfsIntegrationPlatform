USE [Tfs_IntegrationPlatform]


GO
/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed before the build script	
 Use SQLCMD syntax to include a file into the pre-deployment script			
 Example:      :r .\filename.sql								
 Use SQLCMD syntax to reference a variable in the pre-deployment script		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

GO

GO
PRINT N'Dropping FriendlyName...';


GO
EXECUTE sp_dropextendedproperty @name = N'FriendlyName';


GO
PRINT N'Dropping ReferenceName...';


GO
EXECUTE sp_dropextendedproperty @name = N'ReferenceName';


GO
PRINT N'Creating dbo.MigrationSourceNotUsedInExistingSessions...';


GO
CREATE FUNCTION [dbo].[MigrationSourceNotUsedInExistingSessions]
(@LeftSourceId INT, @RightSourceId INT)
RETURNS INT
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


GO
PRINT N'Creating dbo.chkSingleUsageOfMigrationSourceInSessions...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS]
    ADD CONSTRAINT [chkSingleUsageOfMigrationSourceInSessions] CHECK ([dbo].[MigrationSourceNotUsedInExistingSessions](LeftSourceId, RightSourceId) = 0);


GO
PRINT N'Creating FriendlyName...';


GO
EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v1.3';


GO
PRINT N'Creating ReferenceName...';


GO
EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = 'EED75629-45C5-4b6c-85D2-C379FD2C9874';

GO

