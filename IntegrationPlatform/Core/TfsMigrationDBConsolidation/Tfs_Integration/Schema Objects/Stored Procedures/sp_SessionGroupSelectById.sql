--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupSelectById
--GO

CREATE PROCEDURE dbo.SessionGroupSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, GroupUniqueId, FriendlyName
FROM            SESSION_GROUPS
WHERE        (Id = @Id)
GO

