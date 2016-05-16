--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectBySessionGroupId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectBySessionGroupId
--GO

CREATE PROCEDURE dbo.SessionGroupConfigSelectBySessionGroupId
(
	@SessionGroupId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (SessionGroupId = @SessionGroupId)
GO

