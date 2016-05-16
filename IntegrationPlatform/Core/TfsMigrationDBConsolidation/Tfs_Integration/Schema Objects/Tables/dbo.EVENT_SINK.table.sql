CREATE TABLE [dbo].[EVENT_SINK]
(
	Id int NOT NULL identity(1,1), 
	FriendlyName nvarchar(128) NOT NULL,
	ProviderId int NOT NULL,
	CreationTime datetime NOT NULL,
	SettingXml xml NULL,
	SettingXmlSchema xml NULL
);
