--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupUpdate
--GO

CREATE PROCEDURE dbo.SessionGroupUpdate
(
	@FriendlyName nvarchar(128),
	@Original_Id int,
	@Original_GroupUniqueId uniqueidentifier
)
AS
	SET NOCOUNT OFF;
UPDATE       SESSION_GROUPS
SET                FriendlyName = @FriendlyName
WHERE        (Id = @Original_Id) AND (GroupUniqueId = @Original_GroupUniqueId);
	 
SELECT Id, GroupUniqueId, FriendlyName FROM SESSION_GROUPS WHERE (Id = @Original_Id)
GO

