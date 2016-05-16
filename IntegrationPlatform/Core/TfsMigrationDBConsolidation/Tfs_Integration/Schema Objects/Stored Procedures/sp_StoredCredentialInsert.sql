--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'StoredCredentialInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.StoredCredentialInsert
--GO

CREATE PROCEDURE dbo.StoredCredentialInsert
(
	@CredentialString nvarchar(300),
	@MigrationSourceId int
)
AS
	SET NOCOUNT OFF;
INSERT INTO STORED_CREDENTIALS
                         (CredentialString, MigrationSourceId)
VALUES        (@CredentialString,@MigrationSourceId);
	 
SELECT Id, CredentialString, MigrationSourceId FROM STORED_CREDENTIALS WHERE (Id = SCOPE_IDENTITY())
GO

