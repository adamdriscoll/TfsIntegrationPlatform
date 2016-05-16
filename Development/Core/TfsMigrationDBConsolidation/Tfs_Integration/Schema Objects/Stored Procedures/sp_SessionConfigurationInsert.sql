--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationInsert
--GO

CREATE PROCEDURE dbo.SessionConfigurationInsert
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

