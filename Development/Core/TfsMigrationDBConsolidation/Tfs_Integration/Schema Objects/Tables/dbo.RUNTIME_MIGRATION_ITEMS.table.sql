CREATE TABLE [dbo].[RUNTIME_MIGRATION_ITEMS]
(
	Id bigint identity(1,1) NOT NULL, 
	SourceId int NOT NULL,
	ItemId NVARCHAR(300) NOT NULL,
	ItemVersion NVARCHAR(50) NOT NULL,
	ItemData xml NULL
);
