--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigsSelectBySourceIdAndCreationTime' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigsSelectBySourceIdAndCreationTime
--GO

CREATE PROCEDURE dbo.MigrationSourceConfigSelectBySourceIdAndCreationTime
(
	@CreationTime datetime,
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (CreationTime = @CreationTime) AND (MigrationSourceId = @MigrationSourceId)
GO

