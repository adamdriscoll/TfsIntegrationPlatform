CREATE TABLE [dbo].[SESSION_CONFIGURATIONS]
(
	Id int NOT NULL identity(1,1), 
	SessionUniqueId uniqueidentifier NOT NULL,
	FriendlyName nvarchar(128) NOT NULL,
	SessionGroupConfigId int NOT NULL,
	CreationTime datetime NOT NULL,
	Creator nvarchar(50) NULL,
	DeprecationTime datetime NULL,
	LeftSourceConfigId int NOT NULL,
	RightSourceConfigId int NOT NULL,
	Type int NOT NULL,
	SettingXml xml NULL,
	SettingXmlSchema xml NULL
);
