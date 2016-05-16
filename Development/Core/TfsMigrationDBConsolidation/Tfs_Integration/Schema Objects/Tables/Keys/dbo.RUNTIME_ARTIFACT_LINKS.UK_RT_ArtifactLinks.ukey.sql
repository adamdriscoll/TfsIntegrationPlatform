ALTER TABLE [dbo].[RUNTIME_ARTIFACT_LINKS]
ADD CONSTRAINT [UK_RT_ArtifactLinks]
UNIQUE (SourceSideMigrationSourceId, SourceSideArtifactUrl)