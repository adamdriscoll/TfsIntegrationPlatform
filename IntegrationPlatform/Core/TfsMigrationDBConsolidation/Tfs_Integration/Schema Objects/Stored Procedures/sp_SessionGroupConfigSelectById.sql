--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectById
--GO

CREATE PROCEDURE dbo.SessionGroupConfigSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (Id = @Id)
GO

