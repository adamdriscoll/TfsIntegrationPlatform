CREATE TABLE [dbo].[LINK_ARTIFACT_LINK]
(
	Id int identity(1,1) NOT NULL primary key, 
	SourceArtifactUri nvarchar(400) NOT NULL,
	TargetArtifactUri nvarchar(400) NOT NULL,
	LinkTypeId int NOT NULL,
	Comment nvarchar(max) NULL,
	SourceArtifactId nvarchar(400) NULL,
	IsLocked bit NULL
);
