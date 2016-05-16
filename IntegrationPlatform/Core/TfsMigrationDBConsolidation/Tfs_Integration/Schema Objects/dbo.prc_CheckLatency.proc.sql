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
