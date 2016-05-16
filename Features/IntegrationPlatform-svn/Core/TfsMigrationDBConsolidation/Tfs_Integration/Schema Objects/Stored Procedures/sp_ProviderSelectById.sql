--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ProviderSelectById' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ProviderSelectById
--GO

CREATE PROCEDURE dbo.ProviderSelectById
(
	@Id int
)
AS
	SET NOCOUNT ON;
SELECT        Id, ReferenceName, FriendlyName
FROM            PROVIDERS
WHERE        (Id = @Id)
GO

