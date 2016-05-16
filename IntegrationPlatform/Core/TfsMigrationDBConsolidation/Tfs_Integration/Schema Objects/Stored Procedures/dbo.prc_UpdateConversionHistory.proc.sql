CREATE PROCEDURE [dbo].[prc_UpdateConversionHistory]
	@SessionRunId int, 
	@SourceMigrationId int, 
	@OtherSideMigrationId int,
	@SourceChangeGroupId bigint , 
	@SourceChangeId NVARCHAR(50), 
	@SourceChangeVersion NVARCHAR(50), 
	@OtherSideChangeId NVARCHAR(50), 
	@OtherSideChangeVersion NVARCHAR(50), 
	@ExecutionOrder bigint,
	@UtcWhen DATETIME,
	@Comment NVARCHAR(MAX)
AS
-- Insert into conversion history
	DECLARE @ConvHistoryId BIGINT;
	INSERT INTO dbo.RUNTIME_CONVERSION_HISTORY (SessionRunId, SourceMigrationSourceId, SourceChangeGroupId, UtcWhen, Comment)
		VALUES (@SessionRunId, @SourceMigrationId, @SourceChangeGroupId, @UtcWhen, @Comment)
	SELECT @ConvHistoryId = @@IDENTITY;
	
	EXECUTE CreateItemRevisionPair @ConvHistoryId, @SourceMigrationId, @SourceChangeId, @SourceChangeVersion, 
								   @OtherSideMigrationId, @OtherSideChangeId, @OtherSideChangeVersion;
	
-- Mark the delta table entry as delta synced	
	UPDATE RUNTIME_CHANGE_GROUPS
	SET Status = 8 -- DeltaSynced
	WHERE SessionRunId = @SessionRunId AND SourceMigrationSourceId <> @SourceMigrationId AND ExecutionOrder = @ExecutionOrder
	
	SELECT top(1) * FROM dbo.RUNTIME_CONVERSION_HISTORY
	WHERE SessionRunId = @SessionRunId AND SourceMigrationSourceId = @SourceMigrationId
RETURN 0;