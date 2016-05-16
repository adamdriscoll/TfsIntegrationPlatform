USE [Tfs_IntegrationPlatform]


GO
PRINT N'Dropping [FriendlyName]...';


GO
EXECUTE sp_dropextendedproperty @name = N'FriendlyName';


GO
PRINT N'Dropping [ReferenceName]...';


GO
EXECUTE sp_dropextendedproperty @name = N'ReferenceName';


GO
PRINT N'Dropping chkSingleUsageOfMigrationSourceInSessions...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS] DROP CONSTRAINT [chkSingleUsageOfMigrationSourceInSessions];


GO
PRINT N'Altering [dbo].[RELATED_ARTIFACTS_RECORDS]...';


GO
ALTER TABLE [dbo].[RELATED_ARTIFACTS_RECORDS]
    ADD [OtherProperty] INT NULL;


GO
PRINT N'Altering [dbo].[SESSION_GROUP_CONFIGS]...';


GO
ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS]
    ADD [AddinsConfig] XML NULL;


GO
PRINT N'Altering [dbo].[ConfigCheckoutRecordInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordInsert
--GO

ALTER PROCEDURE dbo.ConfigCheckoutRecordInsert
(
	@SessionGroupConfigId int,
	@CheckOutToken uniqueidentifier
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[CONFIG_CHECKOUT_RECORDS] ([SessionGroupConfigId], [CheckOutToken]) VALUES (@SessionGroupConfigId, @CheckOutToken);
	
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO
PRINT N'Altering [dbo].[ConfigCheckoutRecordUpdateLock]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordUpdateLock' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordUpdateLock
--GO

ALTER PROCEDURE dbo.ConfigCheckoutRecordUpdateLock
(
	@CheckOutToken uniqueidentifier,
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT OFF;
UPDATE       CONFIG_CHECKOUT_RECORDS
SET                CheckOutToken = @CheckOutToken
WHERE        (SessionGroupConfigId = @SessionGroupConfigId);
	 
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO
PRINT N'Altering [dbo].[ConfigCheckoutRecordUpdateUnlock]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordUpdateUnlock' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordUpdateUnlock
--GO

ALTER PROCEDURE dbo.ConfigCheckoutRecordUpdateUnlock
(
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT OFF;
UPDATE       CONFIG_CHECKOUT_RECORDS
SET                CheckOutToken = NULL
WHERE        (SessionGroupConfigId = @SessionGroupConfigId );
	 
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO
PRINT N'Altering [dbo].[EventSinkInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkInsert
--GO

ALTER PROCEDURE dbo.EventSinkInsert
(
	@FriendlyName nvarchar(128),
	@ProviderId int,
	@CreationTime datetime,
	@SettingXml xml,
	@SettingXmlSchema xml
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[EVENT_SINK] ([FriendlyName], [ProviderId], [CreationTime], [SettingXml], [SettingXmlSchema]) VALUES (@FriendlyName, @ProviderId, @CreationTime, @SettingXml, @SettingXmlSchema);
	
SELECT Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema FROM EVENT_SINK WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Altering [dbo].[EventSinkJuncInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkJuncInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkJuncInsert
--GO

ALTER PROCEDURE dbo.EventSinkJuncInsert
(
	@EventSinkId int,
	@SessionConfigId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[EVENT_SINK_JUNC] ([EventSinkId], [SessionConfigId]) VALUES (@EventSinkId, @SessionConfigId);
	
SELECT EventSinkId, SessionConfigId FROM EVENT_SINK_JUNC WHERE (EventSinkId = @EventSinkId) AND (SessionConfigId = @SessionConfigId)
GO
PRINT N'Altering [dbo].[EventSinkJuncSelectBySessionConfigId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkJuncSelectBySessionConfigId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkJuncSelectBySessionConfigId
--GO

ALTER PROCEDURE dbo.EventSinkJuncSelectBySessionConfigId
(
	@SessionConfigId int
)
AS
	SET NOCOUNT ON;
SELECT        EventSinkId, SessionConfigId
FROM            EVENT_SINK_JUNC
WHERE        (SessionConfigId = @SessionConfigId)
GO
PRINT N'Altering [dbo].[EventSinkSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkSelectById
--GO

ALTER PROCEDURE dbo.EventSinkSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema
FROM            EVENT_SINK
WHERE        (Id = @Id)
GO
PRINT N'Altering [dbo].[EventSinkSelectByProviderAndCreationTime]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkSelectByProviderAndCreationTime' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkSelectByProviderAndCreationTime
--GO

ALTER PROCEDURE dbo.EventSinkSelectByProviderAndCreationTime
(
	@ProviderId int,
	@CreationTime datetime
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema
FROM            EVENT_SINK
WHERE        (ProviderId = @ProviderId) AND (CreationTime = @CreationTime)
GO
PRINT N'Altering [dbo].[EventSinkUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkUpdate
--GO

ALTER PROCEDURE dbo.EventSinkUpdate
(
	@FriendlyName nvarchar(128),
	@SettingXml xml,
	@SettingXmlSchema xml,
	@Original_ProviderId int,
	@Original_CreationTime datetime
)
AS
	SET NOCOUNT OFF;
UPDATE       EVENT_SINK
SET                FriendlyName = @FriendlyName, SettingXml = @SettingXml, SettingXmlSchema = @SettingXmlSchema
WHERE        (ProviderId = @Original_ProviderId) AND (CreationTime = @Original_CreationTime);
	 
SELECT Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema FROM EVENT_SINK WHERE (ProviderId = @Original_ProviderId) AND (CreationTime = @Original_CreationTime)
GO
PRINT N'Altering [dbo].[MigrationSourceConfigInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigInsert
--GO

ALTER PROCEDURE dbo.MigrationSourceConfigInsert
(
	@CreationTime datetime,
	@MigrationSourceId int,
	@SettingXml xml,
	@SettingXmlSchema xml
)
AS
	SET NOCOUNT OFF;
INSERT INTO MIGRATION_SOURCE_CONFIGS
                         (CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema)
VALUES        (@CreationTime,@MigrationSourceId,@SettingXml,@SettingXmlSchema);
	 
SELECT Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema FROM MIGRATION_SOURCE_CONFIGS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Altering [dbo].[MigrationSourceConfigSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigSelectById
--GO

ALTER PROCEDURE dbo.MigrationSourceConfigSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (Id = @Id)
GO
PRINT N'Altering [dbo].[MigrationSourceConfigSelectByMigrationSourceId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigSelectByMigrationSourceId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigSelectByMigrationSourceId
--GO

ALTER PROCEDURE dbo.MigrationSourceConfigSelectByMigrationSourceId
(
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (MigrationSourceId = @MigrationSourceId)
GO
PRINT N'Altering [dbo].[MigrationSourceConfigSelectBySourceIdAndCreationTime]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceConfigsSelectBySourceIdAndCreationTime' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceConfigsSelectBySourceIdAndCreationTime
--GO

ALTER PROCEDURE dbo.MigrationSourceConfigSelectBySourceIdAndCreationTime
(
	@CreationTime datetime,
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, MigrationSourceId, SettingXml, SettingXmlSchema
FROM            MIGRATION_SOURCE_CONFIGS
WHERE        (CreationTime = @CreationTime) AND (MigrationSourceId = @MigrationSourceId)
GO
PRINT N'Altering [dbo].[MigrationSourceInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceInsert
--GO

ALTER PROCEDURE dbo.MigrationSourceInsert
(
	@FriendlyName nvarchar(128),
	@ServerIdentifier nvarchar(128),
	@ServerUrl nvarchar(400),
	@SourceIdentifier nvarchar(400),
	@ProviderId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO MIGRATION_SOURCES
                         (FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId)
VALUES        (@FriendlyName,@ServerIdentifier,@ServerUrl,@SourceIdentifier,@ProviderId);
	 
SELECT Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId FROM MIGRATION_SOURCES WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Altering [dbo].[MigrationSourceSelectByConfigInfo]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceSelectByConfigInfo' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceSelectByConfigInfo
--GO

ALTER PROCEDURE dbo.MigrationSourceSelectByConfigInfo
(
	@ServerIdentifier nvarchar(128),
	@SourceIdentifier nvarchar(400),
	@ProviderId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId
FROM            MIGRATION_SOURCES
WHERE        (ServerIdentifier = @ServerIdentifier) AND (SourceIdentifier = @SourceIdentifier) AND (ProviderId = @ProviderId)
GO
PRINT N'Altering [dbo].[MigrationSourceSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceSelectById
--GO

ALTER PROCEDURE dbo.MigrationSourceSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId
FROM            MIGRATION_SOURCES
WHERE        (Id = @Id)
GO
PRINT N'Altering [dbo].[MigrationSourceUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceUpdate
--GO

ALTER PROCEDURE dbo.MigrationSourceUpdate
(
	@FriendlyName nvarchar(128),
	@ServerUrl nvarchar(400),
	@Original_Id int
)
AS
	SET NOCOUNT OFF;
UPDATE       MIGRATION_SOURCES
SET                FriendlyName = @FriendlyName, ServerUrl = @ServerUrl
WHERE        (Id = @Original_Id);
	 
SELECT Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId FROM MIGRATION_SOURCES WHERE (Id = @Original_Id)
GO
PRINT N'Altering [dbo].[prc_ResetChangeGroupsAfterResolve]...';


GO
ALTER PROCEDURE [dbo].[prc_ResetChangeGroupsAfterResolve]
	@SessionUniqueId uniqueidentifier,
	@SourceUniqueId uniqueidentifier,
	@DeltaTableExecutionOrder bigint
AS
-- Reset all later delta table entries back to deltapending	
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 1 -- DeltaPending
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND ExecutionOrder > @DeltaTableExecutionOrder
	
-- Reset all unprocessed migration instructions
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 10 -- Obsolete
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND Status = 4 OR Status = 5 OR Status = 9 -- Pending or InProgress or PendingConflictDetection
	
	SELECT * FROM RUNTIME_CHANGE_GROUPS
	WHERE SessionUniqueId = @SessionUniqueId AND SourceUniqueId = @SourceUniqueId AND ExecutionOrder = @DeltaTableExecutionOrder

RETURN 0;
GO
PRINT N'Altering [dbo].[prc_Stage1DBCleanup]...';


GO
--Stage One DB cleanup: remove the completed migration instruction ChangeActions

ALTER PROCEDURE [dbo].[prc_Stage1DBCleanup]
AS
	DECLARE @DeletionChangeActionId TABLE(Id BIGINT)

	-- RETRIEVE FIRST BATCH
	INSERT INTO @DeletionChangeActionId(Id)
	SELECT TOP 10000 a.ChangeActionId
	FROM dbo.RUNTIME_CHANGE_ACTION AS a
	INNER JOIN dbo.RUNTIME_CHANGE_GROUPS AS g
	   ON g.Id = a.ChangeGroupId
	WHERE (g.Status = 6 AND g.ContainsBackloggedAction = 0)


	-- SKIP DELETION IF THERE ARE NOT *MANY* JUNK IN DB
	WHILE (SELECT COUNT(*) FROM @DeletionChangeActionId) = 10000
	BEGIN
		PRINT CONVERT(VARCHAR(12),GETDATE(),114) + N' - Deleting 10000 RUNTIME_CHANGE_ACTION rows'
		DELETE FROM dbo.RUNTIME_CHANGE_ACTION
		WHERE ChangeActionId IN (SELECT Id FROM @DeletionChangeActionId)
		
		-- RETRIEVE NEXT BATCH
		DELETE FROM @DeletionChangeActionId	
		
		INSERT INTO @DeletionChangeActionId(Id)
		SELECT TOP 10000 a.ChangeActionId
		FROM dbo.RUNTIME_CHANGE_ACTION AS a
		INNER JOIN dbo.RUNTIME_CHANGE_GROUPS AS g
		   ON g.Id = a.ChangeGroupId
		WHERE (g.Status = 6 AND g.ContainsBackloggedAction = 0)	
	END
RETURN 0;
GO
PRINT N'Altering [dbo].[prc_UpdateConversionHistory]...';


GO
ALTER PROCEDURE [dbo].[prc_UpdateConversionHistory]
	@SessionRunId int, 
	@SourceMigrationId int, 
	@OtherSideMigrationId int,
	@SourceChangeGroupId bigint , 
	@SourceChangeId NVARCHAR(50), 
	@SourceChangeVersion NVARCHAR(50), 
	@OtherSideChangeId NVARCHAR(50), 
	@OtherSideChangeVersion NVARCHAR(50), 
	@ExecutionOrder bigint,
	@UtcWhen DATETIME,
	@Comment NVARCHAR(MAX)
AS
-- Insert into conversion history
	DECLARE @ConvHistoryId BIGINT;
	INSERT INTO dbo.RUNTIME_CONVERSION_HISTORY (SessionRunId, SourceMigrationSourceId, SourceChangeGroupId, UtcWhen, Comment)
		VALUES (@SessionRunId, @SourceMigrationId, @SourceChangeGroupId, @UtcWhen, @Comment)
	SELECT @ConvHistoryId = @@IDENTITY;
	
	EXECUTE CreateItemRevisionPair @ConvHistoryId, @SourceMigrationId, @SourceChangeId, @SourceChangeVersion, 
								   @OtherSideMigrationId, @OtherSideChangeId, @OtherSideChangeVersion;
	
-- Mark the delta table entry as delta synced	
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 8 -- DeltaSynced
	WHERE SessionRunId = @SessionRunId AND SourceMigrationSourceId <> @SourceMigrationId AND ExecutionOrder = @ExecutionOrder
	
	SELECT top(1) * FROM dbo.RUNTIME_CONVERSION_HISTORY
	WHERE SessionRunId = @SessionRunId AND SourceMigrationSourceId = @SourceMigrationId
RETURN 0;
GO
PRINT N'Altering [dbo].[ProviderInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderInsert
--GO

ALTER PROCEDURE dbo.ProviderInsert
(
	@ReferenceName uniqueidentifier,
	@FriendlyName nvarchar(128)
)
AS
	SET NOCOUNT OFF;
INSERT INTO PROVIDERS
                         (ReferenceName, FriendlyName)
VALUES        (@ReferenceName,@FriendlyName);
	 
SELECT Id, ReferenceName, FriendlyName FROM PROVIDERS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Altering [dbo].[ProviderSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderSelectById
--GO

ALTER PROCEDURE dbo.ProviderSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, ReferenceName, FriendlyName
FROM            PROVIDERS
WHERE        (Id = @Id)
GO
PRINT N'Altering [dbo].[ProviderSelectByRegFileName]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderSelectByRegFileName' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderSelectByRegFileName
--GO

ALTER PROCEDURE dbo.ProviderSelectByRegFileName
(
	@ReferenceName uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, ReferenceName, FriendlyName
FROM            PROVIDERS
WHERE        (ReferenceName = @ReferenceName)
GO
PRINT N'Altering [dbo].[ProviderUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderUpdate
--GO

ALTER PROCEDURE dbo.ProviderUpdate
(
	@FriendlyName nvarchar(128),
	@Original_Id int,
	@Original_ReferenceName uniqueidentifier
)
AS
	SET NOCOUNT OFF;
UPDATE       PROVIDERS
SET                FriendlyName = @FriendlyName
WHERE        (Id = @Original_Id) AND (ReferenceName = @Original_ReferenceName);
	 
SELECT Id, ReferenceName, FriendlyName FROM PROVIDERS WHERE (Id = @Original_Id)
GO
PRINT N'Altering [dbo].[SessionConfigurationInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationInsert
--GO

ALTER PROCEDURE dbo.SessionConfigurationInsert
(
	@SessionUniqueId uniqueidentifier,
	@FriendlyName nvarchar(128),
	@SessionGroupConfigId int,
	@CreationTime datetime,
	@Creator nvarchar(50),
	@DeprecationTime datetime,
	@LeftSourceConfigId int,
	@RightSourceConfigId int,
	@Type int,
	@SettingXml xml,
	@SettingXmlSchema xml
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[SESSION_CONFIGURATIONS] ([SessionUniqueId], [FriendlyName], [SessionGroupConfigId], [CreationTime], [Creator], [DeprecationTime], [LeftSourceConfigId], [RightSourceConfigId], [Type], [SettingXml], [SettingXmlSchema]) VALUES (@SessionUniqueId, @FriendlyName, @SessionGroupConfigId, @CreationTime, @Creator, @DeprecationTime, @LeftSourceConfigId, @RightSourceConfigId, @Type, @SettingXml, @SettingXmlSchema);
	
SELECT Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, SettingXml, SettingXmlSchema FROM SESSION_CONFIGURATIONS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Altering [dbo].[SessionConfigurationSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectById
--GO

ALTER PROCEDURE dbo.SessionConfigurationSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (Id = @Id)
GO
PRINT N'Altering [dbo].[SessionConfigurationSelectBySessionGroupConfigId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectBySessionGroupConfigId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectBySessionGroupConfigId
--GO

ALTER PROCEDURE dbo.SessionConfigurationSelectBySessionGroupConfigId
(
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (SessionGroupConfigId = @SessionGroupConfigId)
GO
PRINT N'Altering [dbo].[SessionConfigurationSelectBySessionUniqueId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectBySessionUniqueId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectBySessionUniqueId
--GO

ALTER PROCEDURE dbo.SessionConfigurationSelectBySessionUniqueId
(
	@SessionUniqueId uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (SessionUniqueId = @SessionUniqueId)
GO
PRINT N'Altering [dbo].[SessionGroupConfigInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigInsert
--GO

ALTER PROCEDURE dbo.SessionGroupConfigInsert
(
	@CreationTime datetime,
	@Creator nvarchar(50),
	@DeprecationTime datetime,
	@Status int,
	@SessionGroupId int,
	@LinkingSettingId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO SESSION_GROUP_CONFIGS
                         (CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId)
VALUES        (@CreationTime,@Creator,@DeprecationTime,@Status,@SessionGroupId,@LinkingSettingId);
	 
SELECT Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId FROM SESSION_GROUP_CONFIGS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Altering [dbo].[SessionGroupConfigSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectById
--GO

ALTER PROCEDURE dbo.SessionGroupConfigSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (Id = @Id)
GO
PRINT N'Altering [dbo].[SessionGroupConfigSelectBySessionGroupId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectBySessionGroupId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectBySessionGroupId
--GO

ALTER PROCEDURE dbo.SessionGroupConfigSelectBySessionGroupId
(
	@SessionGroupId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (SessionGroupId = @SessionGroupId)
GO
PRINT N'Altering [dbo].[SessionGroupConfigSelectByStatusAndSessionGroup]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectByStatusAndSessionGroup' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectByStatusAndSessionGroup
--GO

ALTER PROCEDURE dbo.SessionGroupConfigSelectByStatusAndSessionGroup
(
	@SessionGroupId int,
	@Status int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (SessionGroupId = @SessionGroupId) AND (Status = @Status)
GO
PRINT N'Altering [dbo].[SessionGroupInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupInsert
--GO

ALTER PROCEDURE dbo.SessionGroupInsert
(
	@GroupUniqueId uniqueidentifier,
	@FriendlyName nvarchar(128)
)
AS
	SET NOCOUNT OFF;
INSERT INTO SESSION_GROUPS
                         (GroupUniqueId, FriendlyName)
VALUES        (@GroupUniqueId,@FriendlyName);
	 
SELECT Id, GroupUniqueId, FriendlyName FROM SESSION_GROUPS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Altering [dbo].[SessionGroupSelectByGroupUniqueId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupSelectByGroupUniqueId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupSelectByGroupUniqueId
--GO

ALTER PROCEDURE dbo.SessionGroupSelectByGroupUniqueId
(
	@GroupUniqueId uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, GroupUniqueId, FriendlyName
FROM            SESSION_GROUPS
WHERE        (GroupUniqueId = @GroupUniqueId)
GO
PRINT N'Altering [dbo].[SessionGroupSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupSelectById
--GO

ALTER PROCEDURE dbo.SessionGroupSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, GroupUniqueId, FriendlyName
FROM            SESSION_GROUPS
WHERE        (Id = @Id)
GO
PRINT N'Altering [dbo].[SessionGroupUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupUpdate
--GO

ALTER PROCEDURE dbo.SessionGroupUpdate
(
	@FriendlyName nvarchar(128),
	@Original_Id int,
	@Original_GroupUniqueId uniqueidentifier
)
AS
	SET NOCOUNT OFF;
UPDATE       SESSION_GROUPS
SET                FriendlyName = @FriendlyName
WHERE        (Id = @Original_Id) AND (GroupUniqueId = @Original_GroupUniqueId);
	 
SELECT Id, GroupUniqueId, FriendlyName FROM SESSION_GROUPS WHERE (Id = @Original_Id)
GO
PRINT N'Altering [dbo].[StoredCredentialInsert]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialInsert
--GO

ALTER PROCEDURE dbo.StoredCredentialInsert
(
	@CredentialString nvarchar(300),
	@MigrationSourceId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO STORED_CREDENTIALS
                         (CredentialString, MigrationSourceId)
VALUES        (@CredentialString,@MigrationSourceId);
	 
SELECT Id, CredentialString, MigrationSourceId FROM STORED_CREDENTIALS WHERE (Id = SCOPE_IDENTITY())
GO
PRINT N'Altering [dbo].[StoredCredentialSelectById]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialSelectById
--GO

ALTER PROCEDURE dbo.StoredCredentialSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CredentialString, MigrationSourceId
FROM            STORED_CREDENTIALS
WHERE        (Id = @Id)
GO
PRINT N'Altering [dbo].[StoredCredentialSelectByMigrationSourceId]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialSelectByMigrationSourceId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialSelectByMigrationSourceId
--GO

ALTER PROCEDURE dbo.StoredCredentialSelectByMigrationSourceId
(
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CredentialString, MigrationSourceId
FROM            STORED_CREDENTIALS
WHERE        (MigrationSourceId = @MigrationSourceId)
GO
PRINT N'Altering [dbo].[StoredCredentialUpdate]...';


GO
--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialUpdate
--GO

ALTER PROCEDURE dbo.StoredCredentialUpdate
(
	@CredentialString nvarchar(300),
	@Original_MigrationSourceId int
)
AS
	SET NOCOUNT OFF;
UPDATE       STORED_CREDENTIALS
SET                CredentialString = @CredentialString
WHERE        (MigrationSourceId = @Original_MigrationSourceId);
	 
SELECT Id, CredentialString, MigrationSourceId FROM STORED_CREDENTIALS WHERE (MigrationSourceId = @Original_MigrationSourceId)
GO
PRINT N'Altering [dbo].[MigrationSourceNotUsedInExistingSessions]...';


GO
ALTER FUNCTION [dbo].[MigrationSourceNotUsedInExistingSessions]
(
	@LeftSourceId int, 
	@RightSourceId int
)
RETURNS INT -- 0 if neither @LeftSourceId or @RightSourceId is used in an existing Session
            -- 1 in other cases
AS
BEGIN
	DECLARE @LeftSourceUsageCount INT
	DECLARE @RightSourceUsageCount INT
	DECLARE @RetVal INT
	
	SELECT @LeftSourceUsageCount = COUNT(*) 
	FROM [dbo].[RUNTIME_SESSIONS]
	WHERE RightSourceId = @LeftSourceId
	   OR LeftSourceId = @LeftSourceId
	
	-- the only usage should be the session row that this function is checking in CHECK CONSTRAINT
	IF @LeftSourceUsageCount > 1 
	BEGIN
		-- @LeftSourceId is already used by an existing session
		SET @RetVal = 1
	END
	ELSE
	BEGIN
		SELECT @RightSourceUsageCount = COUNT(*) 
		FROM [dbo].[RUNTIME_SESSIONS]
		WHERE RightSourceId = @RightSourceId
		   OR LeftSourceId = @RightSourceId
	
		-- the only usage should be the session row that this function is checking in CHECK CONSTRAINT	   
		IF @RightSourceUsageCount > 1
		BEGIN
			-- @RightSourceId is already used by an existing session
			SET @RetVal = 1
		END
		ELSE
		BEGIN
			SET @RetVal = 0
		END
	END
	
	RETURN @RetVal
END
GO
PRINT N'Creating chkSingleUsageOfMigrationSourceInSessions...';


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH NOCHECK
    ADD CONSTRAINT [chkSingleUsageOfMigrationSourceInSessions] CHECK ([dbo].[MigrationSourceNotUsedInExistingSessions](LeftSourceId, RightSourceId) = 0);


GO
PRINT N'Creating [FriendlyName]...';


GO
EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Synchronization and Migration Database v2.3';


GO
PRINT N'Creating [ReferenceName]...';


GO
EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = 'A380E420-AE04-4019-A065-60BDFD4BF08D';


GO
-- Project upgrade has moved this code to 'Upgraded.extendedproperties.sql'.

GO
PRINT N'Checking existing data against newly created constraints';


GO
USE [Tfs_IntegrationPlatform];


GO
ALTER TABLE [dbo].[RUNTIME_SESSIONS] WITH CHECK CHECK CONSTRAINT [chkSingleUsageOfMigrationSourceInSessions];


GO
