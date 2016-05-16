--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderSelectByRegFileName' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderSelectByRegFileName
--GO

CREATE PROCEDURE dbo.ProviderSelectByRegFileName
(
	@ReferenceName uniqueidentifier
)
AS
	SET NOCOUNT ON;
SELECT        Id, ReferenceName, FriendlyName
FROM            PROVIDERS
WHERE        (ReferenceName = @ReferenceName)
GO

