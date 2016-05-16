--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkJuncSelectBySessionConfigId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkJuncSelectBySessionConfigId
--GO

CREATE PROCEDURE dbo.EventSinkJuncSelectBySessionConfigId
(
	@SessionConfigId int
)
AS
	SET NOCOUNT ON;
SELECT        EventSinkId, SessionConfigId
FROM            EVENT_SINK_JUNC
WHERE        (SessionConfigId = @SessionConfigId)
GO

