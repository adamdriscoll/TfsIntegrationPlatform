--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderInsert
--GO

CREATE PROCEDURE dbo.ProviderInsert
(
	@ReferenceName uniqueidentifier,
	@FriendlyName nvarchar(128)
)
AS
	SET NOCOUNT OFF;
INSERT INTO PROVIDERS
                         (ReferenceName, FriendlyName)
VALUES        (@ReferenceName,@FriendlyName);
	 
SELECT Id, ReferenceName, FriendlyName FROM PROVIDERS WHERE (Id = SCOPE_IDENTITY())
GO

