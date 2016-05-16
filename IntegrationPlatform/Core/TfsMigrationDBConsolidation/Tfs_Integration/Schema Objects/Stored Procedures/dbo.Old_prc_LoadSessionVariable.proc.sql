CREATE PROCEDURE [dbo].[Old_prc_LoadSessionVariable]
	@SessionId nvarchar(max),
	@Variable nvarchar(256)
AS

SELECT TOP 1 Value FROM Old_SessionState
WHERE	SessionId=@SessionId
		AND Variable=@Variable
GO