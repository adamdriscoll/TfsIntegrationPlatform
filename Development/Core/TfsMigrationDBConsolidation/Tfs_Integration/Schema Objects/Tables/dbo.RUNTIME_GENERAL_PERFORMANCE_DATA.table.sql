CREATE TABLE [dbo].[RUNTIME_GENERAL_PERFORMANCE_DATA]
(
	Id int NOT NULL identity(1,1), 
	SessionGroupRunId int NOT NULL,
	SessionUniqueId uniqueidentifier NOT NULL,
	SourceUniqueId uniqueidentifier NOT NULL,
	CriterionReferenceName uniqueidentifier NOT NULL, 
	CriterionFriendlyName nvarchar(50) NOT NULL,
	PerfCounter	bigint NULL,
	PerfStartTime datetime NULL,
	PerfFinishTime datetime NULL
);
