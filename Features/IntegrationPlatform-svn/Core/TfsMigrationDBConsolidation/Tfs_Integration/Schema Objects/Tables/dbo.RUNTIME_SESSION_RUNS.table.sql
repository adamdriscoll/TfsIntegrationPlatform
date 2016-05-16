CREATE TABLE [dbo].[RUNTIME_SESSION_RUNS]
(
	Id int identity(1,1) NOT NULL, 
	ConfigurationId int NOT NULL,
	LeftHighWaterMark nvarchar(128) NULL,
	RightHighWaterMark nvarchar(128) NULL,
	State int NULL,
	IsPreview bit NOT NULL,
	SessionGroupRunId int NOT NULL,
	StartTime datetime NULL,
	EndTime datetime NULL,
	ConflictCollectionId int NOT NULL
);
