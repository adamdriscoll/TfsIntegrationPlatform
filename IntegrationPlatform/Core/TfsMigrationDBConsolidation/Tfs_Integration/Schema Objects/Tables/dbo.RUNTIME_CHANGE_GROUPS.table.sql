CREATE TABLE [dbo].[RUNTIME_CHANGE_GROUPS]
(
	Id bigint identity(1,1) NOT NULL, 
	Name nvarchar(max) NULL,
	ExecutionOrder bigint NOT NULL,
	SourceMigrationSourceId int NOT NULL,
	Owner nvarchar(200) NULL,
	Comment nvarchar(max) NULL,
	RevisionTime datetime NULL,
	StartTime datetime NULL,
	FinishTime datetime NULL,
	SessionUniqueId uniqueidentifier NOT NULL,
	SourceUniqueId uniqueidentifier NOT NULL,
	Status int NOT NULL,
	SessionRunId int NOT NULL,
	ContainsBackloggedAction bit NOT NULL,
	ReflectedChangeGroupId bigint NULL,
	UsePagedActions bit NULL,
	IsForcedSync bit NULL
);
