CREATE TABLE [dbo].[MIGRATION_SOURCE_CONFIGS]
(
	Id int NOT NULL identity(1,1), 
	CreationTime datetime NOT NULL,
	MigrationSourceId int NOT NULL,
	GeneralSettingXml xml NULL,
	SettingXml xml NULL,
	SettingXmlSchema xml NULL
);
