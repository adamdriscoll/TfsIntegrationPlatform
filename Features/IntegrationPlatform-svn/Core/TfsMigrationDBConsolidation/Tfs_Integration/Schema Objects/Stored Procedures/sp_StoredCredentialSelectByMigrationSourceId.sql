--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialSelectByMigrationSourceId' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialSelectByMigrationSourceId
--GO

CREATE PROCEDURE dbo.StoredCredentialSelectByMigrationSourceId
(
	@MigrationSourceId int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CredentialString, MigrationSourceId
FROM            STORED_CREDENTIALS
WHERE        (MigrationSourceId = @MigrationSourceId)
GO

