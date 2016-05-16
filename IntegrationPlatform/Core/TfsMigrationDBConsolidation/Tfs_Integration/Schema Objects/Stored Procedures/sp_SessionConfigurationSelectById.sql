--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionConfigurationSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionConfigurationSelectById
--GO

CREATE PROCEDURE dbo.SessionConfigurationSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, SessionUniqueId, FriendlyName, SessionGroupConfigId, CreationTime, Creator, DeprecationTime, LeftSourceConfigId, RightSourceConfigId, Type, 
                         SettingXml, SettingXmlSchema
FROM            SESSION_CONFIGURATIONS
WHERE        (Id = @Id)
GO

