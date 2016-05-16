CREATE TABLE [dbo].[RUNTIME_CHANGE_ACTION]
(
	ChangeGroupId bigint NOT NULL,
	ChangeActionId bigint identity(1,1) NOT NULL,
	ActionId uniqueidentifier NOT NULL,
	SourceItem xml NOT NULL,
	FromPath nvarchar(max) NULL,
	ToPath nvarchar(max) NOT NULL,
	Recursivity bit NOT NULL,
	ExecutionOrder int NULL,
	IsSubstituted bit NOT NULL,
	Label nvarchar(max) NULL,
	MergeVersionTo nvarchar(max) NULL,
	Version nvarchar(max) NULL,
	ActionData xml NULL,
	ActionComment nvarchar(max) NULL,
	StartTime datetime NULL,
	FinishTime datetime NULL,
	AnalysisPhase int NOT NULL, --0 for computed delta, 1 for in-process analysis, 2 for migration instruction (final result)
	ItemTypeReferenceName nvarchar(400) NOT NULL,
	Backlogged bit NOT NULL
);
