--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkUpdate
--GO

CREATE PROCEDURE dbo.EventSinkUpdate
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



