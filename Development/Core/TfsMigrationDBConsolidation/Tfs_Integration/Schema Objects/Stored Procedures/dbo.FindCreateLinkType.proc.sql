CREATE PROCEDURE [dbo].[FindCreateLinkType]
	@ReferenceName nvarchar(200),
	@DisplayName nvarchar(200),
	@SourceArtifactTypeId int,
	@TargetArtifactTypeId int,
	@ExtendedProperty nvarchar(max),
	@Id int output
AS
	SELECT @Id = Id
	from LINK_LINK_TYPE
	where ReferenceName = @ReferenceName
	and DisplayName = @DisplayName
	and SourceArtifactTypeId = @SourceArtifactTypeId
	and TargetArtifactTypeId = @TargetArtifactTypeId;
	
	if @Id is null
	begin
		insert into LINK_LINK_TYPE (ReferenceName, DisplayName, SourceArtifactTypeId, TargetArtifactTypeId, ExtendedProperty)
			values (@ReferenceName, @DisplayName, @SourceArtifactTypeId, @TargetArtifactTypeId, @ExtendedProperty);
		select @Id = @@identity;
	end
RETURN 0;