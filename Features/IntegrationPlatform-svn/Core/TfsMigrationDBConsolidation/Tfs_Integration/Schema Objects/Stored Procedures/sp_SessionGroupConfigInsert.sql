--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'SessionGroupConfigInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.SessionGroupConfigInsert
--GO

CREATE PROCEDURE dbo.SessionGroupConfigInsert
(
	@CreationTime datetime,
	@Creator nvarchar(50),
	@DeprecationTime datetime,
	@Status int,
	@SessionGroupId int,
	@LinkingSettingId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO SESSION_GROUP_CONFIGS
                         (CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId)
VALUES        (@CreationTime,@Creator,@DeprecationTime,@Status,@SessionGroupId,@LinkingSettingId);
	 
SELECT Id, CreationTime, Creator, DeprecationTime, Status, SessionGroupId, LinkingSettingId FROM SESSION_GROUP_CONFIGS WHERE (Id = SCOPE_IDENTITY())
GO

