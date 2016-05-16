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
PRINT N'Dropping FriendlyName...';


GO
EXECUTE sp_dropextendedproperty @name = N'FriendlyName';


GO
PRINT N'Dropping ReferenceName...';


GO
EXECUTE sp_dropextendedproperty @name = N'ReferenceName';

GO
PRINT N'Dropping dbo.UK_MigrationSources...';


GO
ALTER TABLE [dbo].[MIGRATION_SOURCES] DROP CONSTRAINT [UK_MigrationSources];


GO
PRINT N'Creating FriendlyName...';


GO
EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v1.5';


GO
PRINT N'Creating ReferenceName...';


GO
EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = '0ADD3C9A-0AF5-4764-9112-C01AE0FE10D9';

GO
