--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkJuncInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkJuncInsert
--GO

CREATE PROCEDURE dbo.EventSinkJuncInsert
(
	@EventSinkId int,
	@SessionConfigId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[EVENT_SINK_JUNC] ([EventSinkId], [SessionConfigId]) VALUES (@EventSinkId, @SessionConfigId);
	
SELECT EventSinkId, SessionConfigId FROM EVENT_SINK_JUNC WHERE (EventSinkId = @EventSinkId) AND (SessionConfigId = @SessionConfigId)
GO

