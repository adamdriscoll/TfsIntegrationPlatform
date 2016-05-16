CREATE PROCEDURE [dbo].[prc_QueryConversionHistory]
	@SessionId UNIQUEIDENTIFIER,
	@SourceUniqueId UNIQUEIDENTIFIER,
	@OtherSideChangeId NVARCHAR(50)
AS
	SELECT * FROM [dbo].[RUNTIME_CONVERSION_HISTORY]
	--WHERE [SessionUniqueId]=@SessionId AND
		  --[SourceUniqueId]=@SourceUniqueId AND
		  --[OtherSideChangeId]=@OtherSideChangeId