CREATE TABLE [dbo].[LINK_LINK_CHANGE_ACTIONS]
(
	Id bigint identity(1,1) NOT NULL primary key, 
	SessionGroupUniqueId uniqueidentifier NOT NULL,
	SessionUniqueId uniqueidentifier NOT NULL,
	SourceId uniqueidentifier NOT NULL,
	ActionId uniqueidentifier NOT NULL,
	ArtifactLinkId int NOT NULL,
	--IsDeferred bit NOT NULL,
	Status int NOT NULL, -- 1: DeltaComputed; 2. Translated; 3. ReadyForMigration; 4. Completed
	LinkChangeGroupId bigint NULL,
	ExecutionOrder int NULL,
	Conflicted bit NOT NULL,
	ServerLinkChangeId NVARCHAR(300) NULL, -- Can be set if a LinkProvider can get a unique identifier for a link change from the Server
);
