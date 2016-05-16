--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'EventSinkInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.EventSinkInsert
--GO

CREATE PROCEDURE dbo.EventSinkInsert
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

