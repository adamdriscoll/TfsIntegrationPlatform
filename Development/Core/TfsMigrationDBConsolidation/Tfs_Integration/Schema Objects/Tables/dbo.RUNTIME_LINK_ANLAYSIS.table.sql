CREATE TABLE [dbo].[RUNTIME_LINK_ANALYSIS_RESULTS]
(
	Id int identity(1,1) NOT NULL, 
	SessionGroupRunId int NOT NULL,
	StartTime datetime NULL,
	MillisecsTillFinish int NULL,
	ConflictListId int NOT NULL
);
