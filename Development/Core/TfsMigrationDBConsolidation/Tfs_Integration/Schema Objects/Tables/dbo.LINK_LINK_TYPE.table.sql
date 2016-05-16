CREATE TABLE [dbo].[LINK_LINK_TYPE]
(
	Id int identity(1,1) NOT NULL primary key, 
	ReferenceName nvarchar(200) NOT NULL,
	DisplayName nvarchar(200) NOT NULL,
	SourceArtifactTypeId int NOT NULL,
	TargetArtifactTypeId int NOT NULL,
	ExtendedProperty nvarchar(max) NULL
);
