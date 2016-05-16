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
PRINT N'Dropping extended properties'

GO
EXEC sp_dropextendedproperty N'FriendlyName', NULL, NULL, NULL, NULL, NULL, NULL

GO
EXEC sp_dropextendedproperty N'ReferenceName', NULL, NULL, NULL, NULL, NULL, NULL

GO
PRINT N'Altering [dbo].[prc_QueryContentConflict]'

GO
ALTER PROCEDURE [dbo].[prc_QueryContentConflict]
	@SourceId UNIQUEIDENTIFIER,
	@SessionId UNIQUEIDENTIFIER
AS

CREATE TABLE #Delta (ChangeGroupId bigint, ChangeActionId bigint, ActionId uniqueidentifier, ActionPath nvarchar(max)) -- Temporarily table for delta 
CREATE TABLE #MigrationInstruction (ChangeGroupId bigint, ChangeActionId bigint, ActionId uniqueidentifier, ActionPath nvarchar(max)) -- Temporarily table for migration instruction
CREATE TABLE #ContentConflict (DeltaChangeGroupId bigint, DeltaChangeActionId bigint, MigrationInstructionChangeGroupId bigint, MigrationInstructionChangeActionId bigint, ActionPath nvarchar(max)) -- Temporarily table for Content conflicts

-- Select all change actions into #Delta
INSERT INTO #Delta (ChangeGroupId, ChangeActionId, ActionId, ActionPath)
SELECT CA.[ChangeGroupId]
      ,CA.[ChangeActionId]
      ,CA.[ActionId]
      ,CA.[ToPath]
  FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
  RIGHT JOIN RUNTIME_CHANGE_GROUPS AS CG WITH(NOLOCK)
  ON CA.ChangeGroupId = CG.Id 
  WHERE CG.SessionUniqueId = @SessionId AND CG.SourceUniqueId = @SourceId AND CG.Status =1 -- DeltaPending
  
-- For Rename, also include the rename-from-name
  INSERT INTO #Delta (ChangeGroupId, ChangeActionId, ActionId, ActionPath)
SELECT CA.[ChangeGroupId]
      ,CA.[ChangeActionId]
      ,CA.[ActionId]
      ,CA.[FromPath]
  FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
  RIGHT JOIN RUNTIME_CHANGE_GROUPS AS CG WITH(NOLOCK)
  ON CA.ChangeGroupId = CG.Id 
  WHERE CG.SessionUniqueId = @SessionId AND CG.SourceUniqueId = @SourceId AND CG.Status =1 AND CA.ActionId = '90F9D977-7F2B-4799-9014-786EC62DFC80' -- Rename

-- Select all change actions into #MigrationInstruction
INSERT INTO #MigrationInstruction (ChangeGroupId, ChangeActionId, ActionId, ActionPath)
SELECT CA.[ChangeGroupId]
      ,CA.[ChangeActionId]
      ,CA.[ActionId]
      ,CA.[ToPath]
  FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
  RIGHT JOIN RUNTIME_CHANGE_GROUPS AS CG WITH(NOLOCK)
  ON CA.ChangeGroupId = CG.Id 
  WHERE CG.SessionUniqueId = @SessionId AND CG.SourceUniqueId = @SourceId AND CG.Status = 9 -- PendingConflictDetection
 
 -- For Rename, also include the rename-from-name 
  INSERT INTO #MigrationInstruction (ChangeGroupId, ChangeActionId, ActionId, ActionPath)
SELECT CA.[ChangeGroupId]
      ,CA.[ChangeActionId]
      ,CA.[ActionId]
      ,CA.[FromPath]
  FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
  RIGHT JOIN RUNTIME_CHANGE_GROUPS AS CG WITH(NOLOCK)
  ON CA.ChangeGroupId = CG.Id 
  WHERE CG.SessionUniqueId = @SessionId AND CG.SourceUniqueId = @SourceId AND CG.Status = 9 and CA.ActionId = '90F9D977-7F2B-4799-9014-786EC62DFC80' -- Rename

-- Detect conflicts
INSERT INTO #ContentConflict(DeltaChangeGroupId, DeltaChangeActionId, MigrationInstructionChangeGroupId, MigrationInstructionChangeActionId, ActionPath)
SELECT 
#Delta.ChangeGroupId,
#Delta.ChangeActionId, 
#MigrationInstruction.ChangeGroupId,
#MigrationInstruction.ChangeActionId,
#Delta.ActionPath
FROM #Delta
INNER JOIN #MigrationInstruction
ON #Delta.ActionPath = #MigrationInstruction.ActionPath

-- Update all changegroups ContainsBackloggedAction
UPDATE RUNTIME_CHANGE_GROUPS
SET ContainsBackloggedAction =1
FROM RUNTIME_CHANGE_GROUPS AS CG
JOIN #ContentConflict AS Conflict
ON CG.Id = Conflict.MigrationInstructionChangeGroupId

--SELECT 
--CA.ChangeActionId,
--CA.ChangeGroupId,
--CA.ActionComment,
--CA.ActionData,
--CA.ActionId,
--CA.AnalysisPhase,
--CA.Backlogged,
--CA.ExecutionOrder,
--CA.FinishTime,
--CA.FromPath,
--CA.IsSubstituted,
--CA.ItemTypeReferenceName,
--CA.Label,
--CA.MergeVersionTo,
--CA.Recursivity,
--CA.SourceItem,
--CA.StartTime,
--CA.ToPath,
--CA.Version
--FROM RUNTIME_CHANGE_ACTION AS CA WITH(NOLOCK)
--INNER JOIN #ContentConflict AS Conflict
--ON CA.ChangeActionId = Conflict.MigrationInstructionChangeActionId

SELECT 
  Conflict.MigrationInstructionChangeActionId
  ,Conflict.DeltaChangeActionId
FROM #ContentConflict AS Conflict
ORDER BY MigrationInstructionChangeActionId ASC

GO


GO
/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script		
 Use SQLCMD syntax to include a file into the post-deployment script			
 Example:      :r .\filename.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
EXEC sp_addextendedproperty 
    @name = 'ReferenceName',
    @value = 'D75F0A18-4B53-4640-97DF-9D8620119F46'

GO

EXEC sp_addextendedproperty
    @name = 'FriendlyName',
    @value = 'TFS Synchronization and Migration Database v1.2'

GO
USE [Tfs_IntegrationPlatform]
IF ((SELECT COUNT(*) 
	FROM 
		::fn_listextendedproperty( 'microsoft_database_tools_deploystamp', null, null, null, null, null, null )) 
	> 0)
BEGIN
	EXEC [dbo].sp_dropextendedproperty 'microsoft_database_tools_deploystamp'
END
EXEC [dbo].sp_addextendedproperty 'microsoft_database_tools_deploystamp', N'd8038f4d-620a-4fa8-a18d-832d5468c5d1'

GO


GO
