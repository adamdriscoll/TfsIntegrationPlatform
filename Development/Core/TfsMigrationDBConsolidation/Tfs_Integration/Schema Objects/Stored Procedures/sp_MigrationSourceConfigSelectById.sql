--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigSelectById
--GO

CREATE PROCEDURE dbo.MigrationSourceConfigSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (Id = @Id)
GO

