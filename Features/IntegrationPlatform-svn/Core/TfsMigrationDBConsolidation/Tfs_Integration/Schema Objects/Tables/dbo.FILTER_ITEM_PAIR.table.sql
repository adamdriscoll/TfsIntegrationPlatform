CREATE TABLE [dbo].[FILTER_ITEM_PAIR]
(
    Id int NOT NULL identity(1,1),
	Filter1MigrationSourceReferenceName uniqueidentifier NULL, 
	Filter1 nvarchar(4000) NULL,
	Filter1SnapshotPoint nvarchar(200) NULL,
	Filter1PeerSnapshotPoint nvarchar(200) NULL,
	Filter1MergeScope nvarchar(200) NULL,
	Filter2MigrationSourceReferenceName uniqueidentifier NULL, 
	Filter2 nvarchar(4000) NULL,
	Filter2SnapshotPoint nvarchar(200) NULL,
	Filter2PeerSnapshotPoint nvarchar(200) NULL,
	Filter2MergeScope nvarchar(200) NULL,
	Neglect bit NOT NULL,
	SessionConfigId int NOT NULL
);
