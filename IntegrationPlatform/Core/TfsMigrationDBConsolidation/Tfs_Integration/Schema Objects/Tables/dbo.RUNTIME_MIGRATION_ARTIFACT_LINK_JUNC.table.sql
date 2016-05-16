CREATE TABLE [dbo].[RUNTIME_MIGRATION_ARTIFACT_LINK_JUNC]
(
	LinkMigrationResultId int NOT NULL, 
	ArtifactLinkId int NOT NULL,
	Comments nvarchar(max) NULL
);
