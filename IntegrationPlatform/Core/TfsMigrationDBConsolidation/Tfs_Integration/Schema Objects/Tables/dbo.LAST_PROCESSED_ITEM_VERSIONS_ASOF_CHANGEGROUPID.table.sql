CREATE TABLE [dbo].[LAST_PROCESSED_ITEM_VERSIONS_ASOF_CHANGEGROUPID]
(
	MigrationSourceId uniqueidentifier NOT NULL primary key, 
	ChangeGroupId bigint NOT NULL
);
