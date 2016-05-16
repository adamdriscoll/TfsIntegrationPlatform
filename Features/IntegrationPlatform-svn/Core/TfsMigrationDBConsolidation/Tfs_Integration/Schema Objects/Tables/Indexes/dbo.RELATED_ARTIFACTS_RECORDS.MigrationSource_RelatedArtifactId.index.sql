CREATE INDEX [MigrationSource_RelatedArtifactId]
    ON [dbo].[RELATED_ARTIFACTS_RECORDS] (MigrationSourceId)
	INCLUDE (RelatedArtifactId)


