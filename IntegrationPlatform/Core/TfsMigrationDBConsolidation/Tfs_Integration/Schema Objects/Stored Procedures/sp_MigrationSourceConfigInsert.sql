--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigInsert
--GO

CREATE PROCEDURE dbo.MigrationSourceConfigInsert
(
	@CreationTime datetime,
	@MigrationSourceId int,
	@SettingXml xml,
	@SettingXmlSchema xml
)
AS
	SET NOCOUNT OFF;
INSERT INTO MIGRATION_SOURCE_CONFIGS
                         (CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema)
VALUES        (@CreationTime,@MigrationSourceId,@SettingXml,@SettingXmlSchema);
	 
SELECT Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema FROM MIGRATION_SOURCE_CONFIGS WHERE (Id = SCOPE_IDENTITY())
GO

