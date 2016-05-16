CREATE PROCEDURE [dbo].[FindCreateMigrationItem]
	@SourceId int,
	@ItemId NVARCHAR(50), 
	@ItemVersion NVARCHAR(50), 
	@Id bigint output
AS
	select @Id = Id
	from RUNTIME_MIGRATION_ITEMS
	where SourceId = @SourceId
	and ItemId = @ItemId
	and ItemVersion = @ItemVersion
	
	if @Id is null
	begin
		insert into RUNTIME_MIGRATION_ITEMS (SourceId, ItemId, ItemVersion)
			values (@SourceId, @ItemId, @ItemVersion);
		select @Id = @@identity;
	end
RETURN 0;