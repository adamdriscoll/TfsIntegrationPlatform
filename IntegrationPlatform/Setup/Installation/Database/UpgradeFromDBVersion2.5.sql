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
PRINT N'Altering [dbo].[SESSION_GROUP_CONFIGS]...';


GO
ALTER TABLE [dbo].[SESSION_GROUP_CONFIGS]
    ADD [Settings] XML NULL;


GO
PRINT N'Creating [dbo].[prc_CheckLatency]...';


GO
CREATE PROCEDURE [dbo].[prc_CheckLatency]
	@ThresholdInMinutes int = 30,
	@Subject varchar(128) = 'Sync latency is greater than',
	@SessionGroupId varchar(50) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @UTCOffset int;
	SET @UTCOffset = DATEDIFF (minute, GETUTCDATE(), GETDATE());

	IF @SessionGroupId IS NULL
		SET @SessionGroupId = (SELECT TOP 1 [GroupUniqueId]
	  FROM [Tfs_IntegrationPlatform].[dbo].[SESSION_GROUPS]  
	  -- SessionStatEnum: initialized = 0, running = 1, paused = 2
	where State = 0 or State = 1 or State = 2)
  
	SELECT TOP 1 * FROM
	(
	SELECT  DATEADD(minute, @UTCOffset, [PollTime]) as PollTimePT
		  ,dv.MigrationSourceFriendlyName
		  ,DATEADD(minute, @UTCOffset, [MigrationHWM]) as MigrationHWMPT
		  ,ROUND(CAST(Latency as float) / 60, 0) as LatencyMinutes
		  ,[BacklogCount]
		  ,@Subject + ' ' + CAST(@ThresholdInMinutes as varchar) + ' minutes at ' + CAST(GETDATE() as varchar) as Subject
	  FROM LATENCY_POLL lp
	  LEFT JOIN [Tfs_IntegrationPlatform].[dbo].SessionGroupDetailsView as dv ON lp.MigrationSourceId=dv.MigrationSourceId
	WHERE
		dv.SessionGroupUniqueId= @SessionGroupId
	AND
		lp.Id IN (SELECT MAX(Id) FROM LATENCY_POLL GROUP BY MigrationSourceId)
	) AS blah
	WHERE LatencyMinutes > @ThresholdInMinutes
END
GO
PRINT N'Altering [dbo].[SessionGroupDetailsView]...';


GO
ALTER VIEW [dbo].[SessionGroupDetailsView]
	AS 
SELECT sData.SessionGroupUniqueId AS SessionGroupUniqueId
      ,sData.SessionGroupFriendlyName AS SessionGroupFriendlyName
      ,sData.SessionUniqueId AS SessionUniqueId
      ,sData.SessionFriendlyName AS SessionFriendlyName
      ,sData.SessionType As SessionType
      ,mData.MigrationSourceId AS MigrationSourceId
      ,mData.MigrationSourceUniqueId AS MigrationSourceUniqueId
      ,mData.MigrationSourceFriendlyName As MigrationSourceFriendlyName
FROM
(SELECT SessionData.SessionGroupUniqueId AS SessionGroupUniqueId
       ,SessionData.SessionGroupFriendlyName AS SessionGroupFriendlyName
       ,SessionData.SessionUniqueId AS SessionUniqueId
       ,sc.FriendlyName AS SessionFriendlyName
       ,sc.Type As SessionType
 FROM
  (SELECT sg.[GroupUniqueId] AS SessionGroupUniqueId
      ,sg.[FriendlyName] AS SessionGroupFriendlyName
      ,s.SessionUniqueId AS SessionUniqueId
  FROM [dbo].[SESSION_GROUPS] AS sg
  INNER JOIN [dbo].[RUNTIME_SESSIONS] AS s ON sg.Id = s.SessionGroupId
  GROUP BY sg.[GroupUniqueId]
      ,sg.[FriendlyName]
      ,s.SessionUniqueId) AS SessionData
INNER JOIN [dbo].[SESSION_CONFIGURATIONS] AS sc ON sc.SessionUniqueId = SessionData.SessionUniqueId) As sData
INNER JOIN
(SELECT s.SessionUniqueId AS SessionUniqueId      
      ,ms.Id AS MigrationSourceId
      ,ms.UniqueId AS MigrationSourceUniqueId
      ,ms.FriendlyName As MigrationSourceFriendlyName
  FROM [dbo].[RUNTIME_SESSIONS] AS s
  INNER JOIN [dbo].[MIGRATION_SOURCES] AS ms ON (ms.Id = s.LeftSourceId OR ms.Id = s.RightSourceId)
  GROUP BY s.SessionUniqueId
      ,ms.Id
      ,ms.UniqueId
      ,ms.FriendlyName) AS mData
ON sData.SessionUniqueId = mData.SessionUniqueId
GROUP BY sData.SessionGroupUniqueId
      ,sData.SessionGroupFriendlyName
      ,sData.SessionUniqueId
      ,sData.SessionFriendlyName
      ,sData.SessionType
      ,mData.MigrationSourceId
      ,mData.MigrationSourceUniqueId
      ,mData.MigrationSourceFriendlyName
GO
PRINT N'Creating [FriendlyName]...';


GO
EXECUTE sp_addextendedproperty @name = N'FriendlyName', @value = 'TFS Integration Platform Database v2.6';


GO
PRINT N'Creating [ReferenceName]...';


GO
EXECUTE sp_addextendedproperty @name = N'ReferenceName', @value = '58062E32-D0D9-4127-AE86-B3E5DF6B36B5';


GO
