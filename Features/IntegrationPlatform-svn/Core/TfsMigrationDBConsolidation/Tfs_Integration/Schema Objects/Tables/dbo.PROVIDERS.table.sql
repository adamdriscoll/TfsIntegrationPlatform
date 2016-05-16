CREATE TABLE [dbo].[PROVIDERS]
(
	Id int NOT NULL identity(1,1), 
	ReferenceName uniqueidentifier NOT NULL,
	FriendlyName nvarchar(128) NOT NULL,
	ProviderVersion nvarchar(30) NULL
);
