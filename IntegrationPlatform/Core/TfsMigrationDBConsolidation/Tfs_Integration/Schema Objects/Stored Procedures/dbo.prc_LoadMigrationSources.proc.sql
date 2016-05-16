CREATE PROCEDURE [dbo].[prc_LoadMigrationSources]
	@SourceUniqueId uniqueidentifier
AS
	SELECT * from [dbo].[MIGRATION_SOURCES] 
	WHERE UniqueId = @SourceUniqueId