CREATE PROCEDURE [dbo].[CreateItemRevisionPair]
	@ConversionHistoryId BIGINT,
	@SourceMigrationId INT, 
	@SourceChangeId NVARCHAR(50), 
	@SourceChangeVersion NVARCHAR(50), 
	@OthereSideMigrationId INT,
	@OtherSideChangeId NVARCHAR(50), 
	@OtherSideChangeVersion NVARCHAR(50)
AS
	DECLARE @SourceItemId BIGINT;
	DECLARE @TargetItemId BIGINT;
	DECLARE @ItemRevPairCount INT;
	
	EXECUTE FindCreateMigrationItem @SourceMigrationId, @SourceChangeId, @SourceChangeVersion, @SourceItemId output;
	EXECUTE FindCreateMigrationItem @OthereSideMigrationId, @OtherSideChangeId, @OtherSideChangeVersion, @TargetItemId output;
	
	SELECT @ItemRevPairCount = COUNT(*)
	FROM RUNTIME_ITEM_REVISION_PAIRS
	WHERE ConversionHistoryId = @ConversionHistoryId
	  AND LeftMigrationItemId = @SourceItemId
	  AND RightMigrationItemId = @TargetItemId;
	  
	IF @ItemRevPairCount > 0 BEGIN
		SELECT @ItemRevPairCount = COUNT(*)
		FROM RUNTIME_ITEM_REVISION_PAIRS
		WHERE ConversionHistoryId = @ConversionHistoryId
		  AND LeftMigrationItemId = @TargetItemId
		  AND RightMigrationItemId = @SourceItemId;
	  
	    IF @ItemRevPairCount > 0 BEGIN
			INSERT INTO RUNTIME_ITEM_REVISION_PAIRS(LeftMigrationItemId, RightMigrationItemId, ConversionHistoryId)
				VALUES (@SourceItemId, @TargetItemId, @ConversionHistoryId);
		END
	END
RETURN 0;