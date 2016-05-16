CREATE TABLE [dbo].[SESSION_GROUP_CONFIGS]
(
	Id int NOT NULL identity(1,1),
	CreationTime datetime NOT NULL,
	Creator nvarchar(50) NULL,
	DeprecationTime datetime NULL,
	Status int NOT NULL,
	SessionGroupId int NOT NULL,
	LinkingSettingId int NULL,
	FriendlyName nvarchar(300) NULL,
	UniqueId uniqueidentifier NOT NULL,
	WorkFlowType int NOT NULL,--                                   Frequency  |  DirectionOfFlow  |  SyncContext
							  -- 0 for OneDirectionalMigration ==> ContinuousManual + Unidirectional + Unidirectional
	                          -- 1 for BidirectionalMigration  ==> ContinuousManual + Bidirectional + Bidirectional
	                          -- 2 for OneDirectionalSynchronization ==> ContinuousAutomatic + Unidirectional + Unidirectional
	                          -- 3 for BidirectionalSynchronization  ==> ContinuousAutomatic + Bidirectional 
	                          -- 4 for OneDirectionalSynchronizationWithoutContextSync	 ==> ContinuousAutomatic + Unidirectional + Disabled
		                      -- 5 for BidirectionalSynchronizationWithOneWayContextSync ==> ContinuousAutomatic + Bidirectional + Unidirectional
	UserIdentityMappingsConfig xml NULL,
	ErrorManagementConfig xml NULL,
	AddinsConfig xml NULL,
	Settings xml NULL,
);
