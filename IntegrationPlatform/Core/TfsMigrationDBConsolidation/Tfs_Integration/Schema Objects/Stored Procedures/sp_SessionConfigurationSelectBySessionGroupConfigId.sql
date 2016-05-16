--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectBySessionGroupConfigId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectBySessionGroupConfigId
--GO

CREATE PROCEDURE dbo.SessionConfigurationSelectBySessionGroupConfigId
(
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (SessionGroupConfigId = @SessionGroupConfigId)
GO

