CREATE TABLE [dbo].[LINK_LINK_CHANGE_GROUPS]
(
	Id bigint identity(1,1) NOT NULL primary key, 
	SessionGroupUniqueId uniqueidentifier NOT NULL,
	SessionUniqueId uniqueidentifier NOT NULL,
	SourceId uniqueidentifier NOT NULL,
	GroupName nvarchar(100) NULL,
	Status int NOT NULL, -- 1: Created; 2. InAnalysis; 3. ReadyForMigration; 4. Completed
	ContainsConflictedAction bit NOT NULL,
	Age int,
	RetriesAtCurrAge int,
);
