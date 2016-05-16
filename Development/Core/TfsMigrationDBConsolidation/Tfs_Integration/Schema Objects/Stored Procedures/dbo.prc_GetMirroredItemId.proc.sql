CREATE PROCEDURE [dbo].[prc_GetMirroredItemId]
@ItemId varchar(50) = '-1',
@MigrationSourceId int
AS
BEGIN
	declare @MigrationItemInternalId int
	declare @LeftId bigint
	declare @RightId bigint

	SELECT TOP(1) @MigrationItemInternalId=Id
	FROM [dbo].[RUNTIME_MIGRATION_ITEMS] WITH (NOLOCK)
	WHERE (ItemId = @ItemId AND SourceId = @MigrationSourceId)

	IF @MigrationItemInternalId IS NOT NULL
		BEGIN 
			SELECT TOP(1) @LeftId=LeftMigrationItemId, @RightId=RightMigrationItemId
			FROM [dbo].[RUNTIME_ITEM_REVISION_PAIRS] with (NOLOCK) 
			WHERE (LeftMigrationItemId = @MigrationItemInternalId OR RightMigrationItemId = @MigrationItemInternalId)
			
			DECLARE @MirroredItemId bigint
			
			IF (@LeftId = @MigrationItemInternalId)
			BEGIN 
				set @MirroredItemId = @RightId
			END
			ELSE
			BEGIN
				set @MirroredItemId = @LeftId
			END
			
			select ItemId
			FROM [dbo].[RUNTIME_MIGRATION_ITEMS] WITH (NOLOCK)
			WHERE Id = @MirroredItemId
		END
	ELSE
		BEGIN
			select '' as ItemId
		END
END