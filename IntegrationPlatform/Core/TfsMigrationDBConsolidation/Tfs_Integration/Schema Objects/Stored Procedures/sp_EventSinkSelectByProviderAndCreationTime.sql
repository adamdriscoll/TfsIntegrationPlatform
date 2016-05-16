--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkSelectByProviderAndCreationTime' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkSelectByProviderAndCreationTime
--GO

CREATE PROCEDURE dbo.EventSinkSelectByProviderAndCreationTime
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

