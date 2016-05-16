CREATE PROCEDURE [dbo].[FindCreateArtifactLink]
	@SourceArtifactUri nvarchar(400),
	@TargetArtifactUri nvarchar(400),
	@LinkTypeId int,
	@Comment nvarchar(max),
	@SourceArtifactId nvarchar(400),
	@CreateOnMissing bit,
	@Id int output
AS
	SELECT @Id = Id
	from LINK_ARTIFACT_LINK
	where SourceArtifactUri = @SourceArtifactUri
	and TargetArtifactUri = @TargetArtifactUri
	and LinkTypeId = @LinkTypeId
	and SourceArtifactId = @SourceArtifactId
	
	if @Id is null AND @CreateOnMissing = 1
	begin
		insert into LINK_ARTIFACT_LINK 
			(SourceArtifactUri, TargetArtifactUri, LinkTypeId, Comment, SourceArtifactId)
			values (@SourceArtifactUri, @TargetArtifactUri, @LinkTypeId, @Comment, @SourceArtifactId)
		select @Id = @@identity;
	end
	else
	begin
		update LINK_ARTIFACT_LINK
		set Comment = @Comment
		where Id = @Id
	end
	
	if @Id is null
	begin
		select @Id = -1;
	end
RETURN 0;