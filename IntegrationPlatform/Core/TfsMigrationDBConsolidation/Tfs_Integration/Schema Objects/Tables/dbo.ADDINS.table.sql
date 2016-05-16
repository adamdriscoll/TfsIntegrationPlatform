CREATE TABLE [dbo].[ADDINS]
(
	Id int NOT NULL identity(1,1) PRIMARY KEY, 
	ReferenceName uniqueidentifier NOT NULL,
	FriendlyName nvarchar(128) NOT NULL,
	ProviderVersion nvarchar(30) NULL -- not used for now
)
