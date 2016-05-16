ALTER TABLE [dbo].[MIGRATION_SOURCE_CONFIGS]
ADD CONSTRAINT [UK_MigrationSourceConfigs]
UNIQUE (CreationTime, MigrationSourceId)