CREATE TABLE [dbo].[CONFLICT_RESOLUTION_RULES]
(
	Id int identity(1,1) NOT NULL, 
	ReferenceName uniqueidentifier NOT NULL,
	ConflictTypeId int NOT NULL,
	ResolutionActionId int NOT NULL,
	ScopeId int NOT NULL,
	ScopeInfoUniqueId uniqueidentifier NOT NULL,
	SourceInfoUniqueId uniqueidentifier NOT NULL,
	RuleData xml NOT NULL,
	CreationTime datetime NOT NULL,
	DeprecationTime datetime NULL,
	Status int NOT NULL --Status is Valid (0), Proposed (1), or Deprecated (2)
);
