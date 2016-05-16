--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigSelectByMigrationSourceId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigSelectByMigrationSourceId
--GO

CREATE PROCEDURE dbo.MigrationSourceConfigSelectByMigrationSourceId
(
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (MigrationSourceId = @MigrationSourceId)
GO

