--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialSelectById
--GO

CREATE PROCEDURE dbo.StoredCredentialSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, CredentialString, MigrationSourceId
FROM            STORED_CREDENTIALS
WHERE        (Id = @Id)
GO

