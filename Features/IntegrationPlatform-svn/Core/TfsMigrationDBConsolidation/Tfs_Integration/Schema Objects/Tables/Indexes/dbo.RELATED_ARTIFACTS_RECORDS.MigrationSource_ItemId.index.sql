CREATE INDEX [MigrationSource_ItemId]
    ON [dbo].[RELATED_ARTIFACTS_RECORDS] (MigrationSourceId)
	INCLUDE (ItemId)


