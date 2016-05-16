CREATE TABLE [dbo].[LATENCY_POLL]
(
	[Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[PollTime] datetime NOT NULL,
	[MigrationSourceId] int NOT NULL,
	[MigrationHWM] datetime NOT NULL,
	[Latency] int NOT NULL,
	[BacklogCount] int NOT NULL,
	[LastMigratedChange] nvarchar(max) NULL,
);

