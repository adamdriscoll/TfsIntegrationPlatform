--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceSelectByConfigInfo' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceSelectByConfigInfo
--GO

CREATE PROCEDURE dbo.MigrationSourceSelectByConfigInfo
(
	@ServerIdentifier nvarchar(128),
	@SourceIdentifier nvarchar(400),
	@ProviderId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId
FROM            MIGRATION_SOURCES
WHERE        (ServerIdentifier = @ServerIdentifier) AND (SourceIdentifier = @SourceIdentifier) AND (ProviderId = @ProviderId)
GO

