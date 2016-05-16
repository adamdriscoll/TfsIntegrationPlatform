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
PRINT N'Dropping extended properties'

GO
EXEC sp_dropextendedproperty N'FriendlyName', NULL, NULL, NULL, NULL, NULL, NULL

GO
EXEC sp_dropextendedproperty N'ReferenceName', NULL, NULL, NULL, NULL, NULL, NULL

GO
PRINT N'Dropping foreign keys from [dbo].[RUNTIME_MIGRATION_ITEMS]'

GO
ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS] DROP
CONSTRAINT [FK_RT_MigrationItems]

GO
PRINT N'Dropping constraints from [dbo].[RUNTIME_MIGRATION_ITEMS]'

GO
ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS] DROP CONSTRAINT [UK_RT_MigrationItems]

GO
PRINT N'Dropping index [ItemId_Version_NonCluster] from [dbo].[RUNTIME_MIGRATION_ITEMS]'

GO
DROP INDEX [ItemId_Version_NonCluster] ON [dbo].[RUNTIME_MIGRATION_ITEMS]

GO
PRINT N'Altering [dbo].[RUNTIME_MIGRATION_ITEMS]'

GO
ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS] ALTER COLUMN [ItemId] [nvarchar] (300) NOT NULL

GO
PRINT N'Creating index [ItemId_Version_NonCluster] on [dbo].[RUNTIME_MIGRATION_ITEMS]'

GO
CREATE NONCLUSTERED INDEX [ItemId_Version_NonCluster] ON [dbo].[RUNTIME_MIGRATION_ITEMS] ([ItemId], [ItemVersion]) ON [PRIMARY]

GO
PRINT N'Creating index [Item_Version_NonCluster] on [dbo].[RUNTIME_MIGRATION_ITEMS]'

GO
CREATE NONCLUSTERED INDEX [Item_Version_NonCluster] ON [dbo].[RUNTIME_MIGRATION_ITEMS] ([ItemVersion]) ON [PRIMARY]

GO
PRINT N'Altering [dbo].[SESSION_GROUPS]'

GO
ALTER TABLE [dbo].[SESSION_GROUPS] ADD
[OrchestrationStatus] [int] NULL

GO
PRINT N'Altering [dbo].[RUNTIME_SESSIONS]'

GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS] ADD
[OrchestrationStatus] [int] NULL

GO
PRINT N'Creating [dbo].[RUNTIME_ORCHESTRATION_COMMAND]'

GO
CREATE TABLE [dbo].[RUNTIME_ORCHESTRATION_COMMAND]
(
[Id] [int] NOT NULL IDENTITY(1, 1),
[SessionGroupId] [int] NOT NULL,
[Command] [int] NOT NULL,
[Status] [int] NOT NULL
) ON [PRIMARY]

GO
PRINT N'Creating primary key [PK__RUNTIME_ORCHESTR__6BE40491] on [dbo].[RUNTIME_ORCHESTRATION_COMMAND]'

GO
ALTER TABLE [dbo].[RUNTIME_ORCHESTRATION_COMMAND] ADD CONSTRAINT [PK__RUNTIME_ORCHESTR__6BE40491] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]

GO
PRINT N'Creating [dbo].[LATENCY_POLL]'

GO
CREATE TABLE [dbo].[LATENCY_POLL]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[PollTime] [datetime] NOT NULL,
[MigrationSourceId] [int] NOT NULL,
[MigrationHWM] [datetime] NOT NULL,
[Latency] [int] NOT NULL,
[BacklogCount] [int] NOT NULL
) ON [PRIMARY]

GO
PRINT N'Creating primary key [PK__LATENCY_POLL__58D1301D] on [dbo].[LATENCY_POLL]'

GO
ALTER TABLE [dbo].[LATENCY_POLL] ADD CONSTRAINT [PK__LATENCY_POLL__58D1301D] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]

GO
PRINT N'Creating index [LeftMigrationItemId] on [dbo].[RUNTIME_ITEM_REVISION_PAIRS]'

GO
CREATE NONCLUSTERED INDEX [LeftMigrationItemId] ON [dbo].[RUNTIME_ITEM_REVISION_PAIRS] ([LeftMigrationItemId]) ON [PRIMARY]

GO
PRINT N'Creating index [RightMigrationItemId] on [dbo].[RUNTIME_ITEM_REVISION_PAIRS]'

GO
CREATE NONCLUSTERED INDEX [RightMigrationItemId] ON [dbo].[RUNTIME_ITEM_REVISION_PAIRS] ([RightMigrationItemId]) ON [PRIMARY]

GO
PRINT N'Adding constraints to [dbo].[RUNTIME_MIGRATION_ITEMS]'

GO
ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS] ADD CONSTRAINT [UK_RT_MigrationItems] UNIQUE NONCLUSTERED  ([SourceId], [ItemId], [ItemVersion]) ON [PRIMARY]

GO
PRINT N'Adding foreign keys to [dbo].[LATENCY_POLL]'

GO
ALTER TABLE [dbo].[LATENCY_POLL] ADD
CONSTRAINT [LATENCY_POLL.FK_MigrationSource.fkey] FOREIGN KEY ([MigrationSourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id])

GO
PRINT N'Adding foreign keys to [dbo].[RUNTIME_ORCHESTRATION_COMMAND]'

GO
ALTER TABLE [dbo].[RUNTIME_ORCHESTRATION_COMMAND] ADD
CONSTRAINT [FK_OrchCmd_to_SessionGroup] FOREIGN KEY ([SessionGroupId]) REFERENCES [dbo].[SESSION_GROUPS] ([Id]) ON DELETE CASCADE

GO
PRINT N'Adding foreign keys to [dbo].[RUNTIME_MIGRATION_ITEMS]'

GO
ALTER TABLE [dbo].[RUNTIME_MIGRATION_ITEMS] ADD
CONSTRAINT [FK_RT_MigrationItems] FOREIGN KEY ([SourceId]) REFERENCES [dbo].[MIGRATION_SOURCES] ([Id])

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
    @value = '2F74C285-001E-47b9-80A6-2AEF812EA8F7'

GO

EXEC sp_addextendedproperty
    @name = 'FriendlyName',
    @value = 'TFS Synchronization and Migration Database v1.1'

GO
USE [Tfs_IntegrationPlatform]
IF ((SELECT COUNT(*) 
	FROM 
		::fn_listextendedproperty( 'microsoft_database_tools_deploystamp', null, null, null, null, null, null )) 
	> 0)
BEGIN
	EXEC [dbo].sp_dropextendedproperty 'microsoft_database_tools_deploystamp'
END
EXEC [dbo].sp_addextendedproperty 'microsoft_database_tools_deploystamp', N'9db650f4-0c2d-4358-b274-80575a3039f4'

GO
