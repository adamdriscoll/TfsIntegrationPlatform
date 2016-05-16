CREATE PROCEDURE [dbo].[FindCreateArtifactType]
	@ReferenceName nvarchar(200),
	@DisplayName nvarchar(200),
	@ArtifactContentType nvarchar(400),
	@Id int output
AS
	select @Id = Id
	from LINK_ARTIFACT_TYPE
	where ReferenceName = @ReferenceName
	and DisplayName = @DisplayName
	and ArtifactContentType = @ArtifactContentType
	
	if @Id is null
	begin
		insert into LINK_ARTIFACT_TYPE (ReferenceName, DisplayName, ArtifactContentType)
			values (@ReferenceName, @DisplayName, @ArtifactContentType);
		select @Id = @@identity;
	end
RETURN 0;