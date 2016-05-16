USE [Tfs_IntegrationPlatform]

GO
PRINT N'Dropping [FriendlyName]...';


GO
EXECUTE sp_dropextendedproperty @name = N'FriendlyName';


GO
PRINT N'Dropping [ReferenceName]...';


GO
EXECUTE sp_dropextendedproperty @name = N'ReferenceName';


GO
PRINT N'Altering [dbo].[prc_QueryContentConflict]...';


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
        AND (CA.ActionId != 'cb71d043-bede-4092-aa87-cf0f14586625' OR CA.ItemTypeReferenceName != 'Microsoft.TeamFoundation.Migration.Toolkit.VersionControlledFolder') -- Add of folder will not be used for conflict detection
  
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
PRINT N'Creating [FriendlyName]...';


GO
EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Integration Platform Database v2.8';


GO
PRINT N'Creating [ReferenceName]...';


GO
EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = 'C4893198-8818-4C3B-8E1E-EECF575F4B92';


GO

