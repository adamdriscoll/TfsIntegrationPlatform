CREATE TABLE [dbo].[FORCE_SYNC_ITEMS]
(
	[Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[SessionId] int NOT NULL,
	[MigrationSourceId] int NOT NULL,
	[ItemId] NVARCHAR(300) NOT NULL,
	[Status] int NULL,
);

