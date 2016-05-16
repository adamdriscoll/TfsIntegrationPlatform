CREATE PROCEDURE [dbo].[prc_LoadChangeAction]
	@ChangeGroupID bigint
AS

SELECT * FROM [dbo].[RUNTIME_CHANGE_ACTION]
WHERE    ChangeGroupId = @ChangeGroupID
ORDER BY [FromPath] ASC
GO
