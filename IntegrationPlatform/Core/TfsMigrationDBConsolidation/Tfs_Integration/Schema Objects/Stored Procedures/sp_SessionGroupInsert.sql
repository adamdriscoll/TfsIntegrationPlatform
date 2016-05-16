--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupInsert
--GO

CREATE PROCEDURE dbo.SessionGroupInsert
(
	@GroupUniqueId uniqueidentifier,
	@FriendlyName nvarchar(128)
)
AS
	SET NOCOUNT OFF;
INSERT INTO SESSION_GROUPS
                         (GroupUniqueId, FriendlyName)
VALUES        (@GroupUniqueId,@FriendlyName);
	 
SELECT Id, GroupUniqueId, FriendlyName FROM SESSION_GROUPS WHERE (Id = SCOPE_IDENTITY())
GO

