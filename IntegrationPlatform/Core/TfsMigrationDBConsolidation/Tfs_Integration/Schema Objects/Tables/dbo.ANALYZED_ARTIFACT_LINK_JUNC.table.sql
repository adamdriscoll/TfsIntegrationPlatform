CREATE TABLE [dbo].[RUNTIME_ANALYSIS_ARTIFACT_LINK_JUNC]
(
	LinkAnalysisResultId int NOT NULL, 
	ArtifactLinkId int NOT NULL,
	IsDeferred bit NOT NULL
);
