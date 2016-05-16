--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupSelectByGroupUniqueId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupSelectByGroupUniqueId
--GO

CREATE PROCEDURE dbo.SessionGroupSelectByGroupUniqueId
(
	@GroupUniqueId uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, GroupUniqueId, FriendlyName
FROM            SESSION_GROUPS
WHERE        (GroupUniqueId = @GroupUniqueId)
GO

