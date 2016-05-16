ALTER TABLE [dbo].[RUNTIME_SESSIONS]
	ADD CONSTRAINT [chkSingleUsageOfMigrationSourceInSessions] 
	CHECK  ([dbo].[MigrationSourceNotUsedInExistingSessions](LeftSourceId, RightSourceId) = 0)
