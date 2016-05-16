CREATE TABLE [dbo].[RUNTIME_CONVERSION_HISTORY]
(
	Id BIGINT IDENTITY(1,1) NOT NULL, 
	SessionRunId int NOT NULL,
	SourceMigrationSourceId int NOT NULL,
	SourceChangeGroupId bigint NULL,
	UtcWhen DATETIME NOT NULL,
	Comment NVARCHAR(MAX) NULL,
	ContentChanged BIT NOT NULL,
);
