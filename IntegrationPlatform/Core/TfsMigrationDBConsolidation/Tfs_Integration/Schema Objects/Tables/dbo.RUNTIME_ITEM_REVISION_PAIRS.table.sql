CREATE TABLE [dbo].[RUNTIME_ITEM_REVISION_PAIRS]
(
	LeftMigrationItemId bigint NOT NULL, 
	RightMigrationItemId bigint NOT NULL, 
	ConversionHistoryId bigint NULL
);
