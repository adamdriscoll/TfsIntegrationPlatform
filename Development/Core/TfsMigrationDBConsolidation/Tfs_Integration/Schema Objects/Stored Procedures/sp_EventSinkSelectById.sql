--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkSelectById
--GO

CREATE PROCEDURE dbo.EventSinkSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ProviderId, CreationTime, SettingXml, SettingXmlSchema
FROM            EVENT_SINK
WHERE        (Id = @Id)
GO

