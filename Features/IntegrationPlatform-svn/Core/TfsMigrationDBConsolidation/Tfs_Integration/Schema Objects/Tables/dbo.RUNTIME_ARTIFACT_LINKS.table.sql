CREATE TABLE [dbo].[RUNTIME_ARTIFACT_LINKS]
(
	Id int NOT NULL, 
	SourceSideMigrationSourceId int NOT NULL,
	SourceSideArtifactUrl nvarchar(400) NOT NULL,
	TargetSideArtifactUrl nvarchar(400) NULL
);
