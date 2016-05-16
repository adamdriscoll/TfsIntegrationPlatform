CREATE PROCEDURE [dbo].[AddVCCloakedPath]
	@Filter1MigrationSource uniqueidentifier,
	@Filter1 nvarchar(4000),
	@Filter2MigrationSource uniqueidentifier,
	@Filter2 nvarchar(4000),
	@SessionConfigUniqueId uniqueidentifier
AS

	DECLARE	@SessionConfigId int
	SELECT @SessionConfigId = Id
	FROM [dbo].[SESSION_GROUP_CONFIGS]
	where UniqueId = @SessionConfigUniqueId

	IF @SessionConfigId IS NOT NULL
	BEGIN
		INSERT INTO [dbo].[FILTER_ITEM_PAIR] (Filter1MigrationSourceReferenceName, Filter1, Filter2MigrationSourceReferenceName, Filter2, Neglect, SessionConfigId)
		VALUES ( @Filter1MigrationSource, @Filter1, @Filter2MigrationSource, @Filter2, 1, @SessionConfigId);

		SELECT * FROM [dbo].[FILTER_ITEM_PAIR] 
		WHERE Id =  IDENT_CURRENT('FILTER_ITEM_PAIR')
		RETURN 0
	END
	ELSE
	BEGIN
		SELECT * FROM [dbo].[FILTER_ITEM_PAIR] 
		WHERE Id =  IDENT_CURRENT('FILTER_ITEM_PAIR')
		RETURN 1
	END
GO
	