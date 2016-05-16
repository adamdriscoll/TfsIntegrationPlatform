CREATE TABLE [dbo].[LAST_PROCESSED_ITEM_VERSIONS]
(
	MigrationSourceId uniqueidentifier NOT NULL, 
	ItemId nvarchar(200) NOT NULL,
	Version nvarchar(200) NOT NULL
);
