--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceInsert
--GO

CREATE PROCEDURE dbo.MigrationSourceInsert
(
	@FriendlyName nvarchar(128),
	@ServerIdentifier nvarchar(128),
	@ServerUrl nvarchar(400),
	@SourceIdentifier nvarchar(400),
	@ProviderId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO MIGRATION_SOURCES
                         (FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId)
VALUES        (@FriendlyName,@ServerIdentifier,@ServerUrl,@SourceIdentifier,@ProviderId);
	 
SELECT Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId FROM MIGRATION_SOURCES WHERE (Id = SCOPE_IDENTITY())
GO

