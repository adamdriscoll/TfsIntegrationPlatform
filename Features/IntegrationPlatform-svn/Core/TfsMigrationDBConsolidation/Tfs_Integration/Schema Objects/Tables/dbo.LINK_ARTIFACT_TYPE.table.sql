CREATE TABLE [dbo].[LINK_ARTIFACT_TYPE]
(
	Id int identity(1,1) NOT NULL primary key, 
	ReferenceName nvarchar(200) NOT NULL,
	DisplayName nvarchar(200) NOT NULL,
	ArtifactContentType nvarchar(400) NOT NULL
);
