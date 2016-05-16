CREATE TABLE [dbo].[RUNTIME_REGISTERED_ACTIONS]
(
	Id int identity(1,1) NOT NULL, 
	ReferenceName uniqueidentifier NOT NULL,
	FriendlyName nvarchar(30) NOT NULL
);
