--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceUpdate
--GO

CREATE PROCEDURE dbo.MigrationSourceUpdate
(
	@FriendlyName nvarchar(128),
	@ServerUrl nvarchar(400),
	@Original_Id int
)
AS
	SET NOCOUNT OFF;
UPDATE       MIGRATION_SOURCES
SET                FriendlyName = @FriendlyName, ServerUrl = @ServerUrl
WHERE        (Id = @Original_Id);
	 
SELECT Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId FROM MIGRATION_SOURCES WHERE (Id = @Original_Id)
GO

