ALTER TABLE [dbo].[RUNTIME_CHANGE_GROUPS]
ADD CONSTRAINT [UK_RT_ChangeGroups]
UNIQUE (ExecutionOrder, SourceMigrationSourceId, Id)