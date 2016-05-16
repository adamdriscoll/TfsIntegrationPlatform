CREATE TABLE [dbo].[MIGRATION_SOURCES]
(
	Id int NOT NULL identity(1,1), 
	UniqueId uniqueidentifier NOT NULL,
	FriendlyName nvarchar(128) NOT NULL,
	ServerIdentifier nvarchar(128) NOT NULL,
	ServerUrl nvarchar(400) NOT NULL,
	SourceIdentifier nvarchar(300) NOT NULL,
	ProviderId int NOT NULL,
	NativeId nvarchar(400) NULL
);
