--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialUpdate
--GO

CREATE PROCEDURE dbo.StoredCredentialUpdate
(
	@CredentialString nvarchar(300),
	@Original_MigrationSourceId int
)
AS
	SET NOCOUNT OFF;
UPDATE       STORED_CREDENTIALS
SET                CredentialString = @CredentialString
WHERE        (MigrationSourceId = @Original_MigrationSourceId);
	 
SELECT Id, CredentialString, MigrationSourceId FROM STORED_CREDENTIALS WHERE (MigrationSourceId = @Original_MigrationSourceId)
GO

