--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigSelectByStatusAndSessionGroup' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigSelectByStatusAndSessionGroup
--GO

CREATE PROCEDURE dbo.SessionGroupConfigSelectByStatusAndSessionGroup
(
	@SessionGroupId int,
	@Status int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId
FROM            SESSION_GROUP_CONFIGS
WHERE        (SessionGroupId = @SessionGroupId) AND (Status = @Status)
GO

