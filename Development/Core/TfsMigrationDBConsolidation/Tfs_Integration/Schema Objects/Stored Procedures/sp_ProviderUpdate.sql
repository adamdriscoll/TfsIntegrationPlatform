--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderUpdate' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderUpdate
--GO

CREATE PROCEDURE dbo.ProviderUpdate
(
	@FriendlyName nvarchar(128),
	@Original_Id int,
	@Original_ReferenceName uniqueidentifier
)
AS
	SET NOCOUNT OFF;
UPDATE       PROVIDERS
SET                FriendlyName = @FriendlyName
WHERE        (Id = @Original_Id) AND (ReferenceName = @Original_ReferenceName);
	 
SELECT Id, ReferenceName, FriendlyName FROM PROVIDERS WHERE (Id = @Original_Id)
GO

