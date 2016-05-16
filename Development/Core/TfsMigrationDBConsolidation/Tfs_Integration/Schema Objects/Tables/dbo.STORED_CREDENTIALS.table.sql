CREATE TABLE [dbo].[STORED_CREDENTIALS]
(
	Id int NOT NULL identity(1,1), 
	CredentialString nvarchar(300) NOT NULL,
	MigrationSourceId int NOT NULL
);
