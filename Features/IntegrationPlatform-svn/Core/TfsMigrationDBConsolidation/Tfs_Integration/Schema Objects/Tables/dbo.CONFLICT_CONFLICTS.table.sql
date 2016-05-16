CREATE TABLE [dbo].[CONFLICT_CONFLICTS]
(
	Id int identity(1,1) NOT NULL, 
	ConflictListId int NOT NULL,
	ConflictTypeId int NOT NULL,
	ConflictDetails nvarchar(max) NULL,
	ConflictedChangeActionId bigint NULL,
	ChangeGroupId bigint NULL,
	ConflictedLinkChangeActionId bigint NULL,
	ConflictedLinkChangeGroupId bigint NULL,
	ScopeId uniqueidentifier NOT NULL,
	SourceMigrationSourceId int NULL,
	ScopeHint nvarchar(max) NULL,
	Status int NOT NULL, -- Unresolved 0; Resolved 1
	ConflictCount int NULL,
	ResolvedByRuleId int NULL,
	CreationTime DATETIME NULL,
);
