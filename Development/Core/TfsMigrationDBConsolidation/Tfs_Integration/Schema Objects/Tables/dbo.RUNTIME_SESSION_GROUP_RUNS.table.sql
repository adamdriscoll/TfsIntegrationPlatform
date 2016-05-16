CREATE TABLE [dbo].[RUNTIME_SESSION_GROUP_RUNS]
(
	Id int identity(1,1) NOT NULL, 
	StartTime datetime NOT NULL,
	EndTime datetime NULL,
	SessionGroupConfigId int NOT NULL,
	ConflictCollectionId int NOT NULL
);
