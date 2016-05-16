--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'MigrationSourceSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.MigrationSourceSelectById
--GO

CREATE PROCEDURE dbo.MigrationSourceSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, FriendlyName, ServerIdentifier, ServerUrl, SourceIdentifier, ProviderId
FROM            MIGRATION_SOURCES
WHERE        (Id = @Id)
GO

