--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectBySessionUniqueId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectBySessionUniqueId
--GO

CREATE PROCEDURE dbo.SessionConfigurationSelectBySessionUniqueId
(
	@SessionUniqueId uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (SessionUniqueId = @SessionUniqueId)
GO

