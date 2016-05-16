CREATE PROCEDURE dbo.EventSinkDelete
(
	@Original_EventSinkId int,
	@Original_SessionConfigId int
)
AS
	SET NOCOUNT OFF;
DELETE FROM EVENT_SINK_JUNC
WHERE        (EventSinkId = @Original_EventSinkId) AND (SessionConfigId = @Original_SessionConfigId)
GO