CREATE INDEX [CONFLICT_CONFLICTS_SourceMigrationSourceId_Status]
    ON [dbo].[CONFLICT_CONFLICTS] 
	(SourceMigrationSourceId,[Status]) 
INCLUDE ([ConflictedChangeActionId], [ChangeGroupId]) 



