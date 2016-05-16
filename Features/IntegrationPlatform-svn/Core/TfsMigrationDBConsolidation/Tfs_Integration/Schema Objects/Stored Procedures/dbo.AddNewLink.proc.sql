CREATE PROCEDURE [dbo].[FindCreateLink]
	--@SessionGroupUniqueId uniqueidentifier,
	--@SessionUniqueId uniqueidentifier,
	--@SourceID uniqueidentifier,
	--@ActionId uniqueidentifier,
	@SourceArtifactId nvarchar(400),
	@SourceArtifactUri nvarchar(400),
	@TargetArtifactUri nvarchar(400),
	@Comment nvarchar(max),
	@LinkTypeReferenceName nvarchar(200),
	@LinkTypeDisplayName nvarchar(200),
	@ExtendedLinkProperty nvarchar(max),
	@SourceArtifactTypeReferenceName nvarchar(200),
	@SourceArtifactTypeDisplayName nvarchar(200),
	@SourceArtifactContentType nvarchar(400),
	@TargetArtifactTypeReferenceName nvarchar(200),
	@TargetArtifactTypeDisplayName nvarchar(200),
	@TargetArtifactContentType nvarchar(400),
	@CreateOnMissing bit
AS
    declare @LinkChangeActionId int;
    declare @SourceArtifactTypeId int;
    declare @TargetArtifactTypeId int;
    declare @LinkTypeId int;
    declare @ArtifactLinkId int;
    
    execute FindCreateArtifactType 
		@SourceArtifactTypeReferenceName, 
		@SourceArtifactTypeDisplayName, 
		@SourceArtifactContentType,
		@SourceArtifactTypeId output;
	
	execute FindCreateArtifactType
		@TargetArtifactTypeReferenceName,
		@TargetArtifactTypeDisplayName,
		@TargetArtifactContentType,
		@TargetArtifactTypeId output;
	
	execute FindCreateLinkType
		@LinkTypeReferenceName,
		@LinkTypeDisplayName,
		@SourceArtifactTypeId,
		@TargetArtifactTypeId,
		@ExtendedLinkProperty,
		@LinkTypeId output;
		
	execute FindCreateArtifactLink
		@SourceArtifactUri,
		@TargetArtifactUri,
		@LinkTypeId,
		@Comment,
		@SourceArtifactId,
		@CreateOnMissing,
		@ArtifactLinkId output;
		
	--select @LinkChangeActionId = Id
	--from LINK_LINK_CHANGE_ACTIONS
	--where SessionGroupUniqueId = @SessionGroupUniqueId
		--and SessionUniqueId = @SessionUniqueId
		--and SourceId = @SourceID
		--and ActionId = @ActionId
		--and ArtifactLinkId = @ArtifactLinkId;
	
	--if @LinkChangeActionId is null
	--begin
		--insert into LINK_LINK_CHANGE_ACTIONS(SessionGroupUniqueId, SessionUniqueId, SourceId, ActionId, ArtifactLinkId, Status, Conflicted)
		--values(@SessionGroupUniqueId, @SessionUniqueId, @SourceID, @ActionId, @ArtifactLinkId, 1, 0); -- default to DeltaComputed and non-conflicted state
		--select @LinkChangeActionId = @@identity
	--end
	
	--SELECT *
	--FROM LINK_LINK_CHANGE_ACTIONS
	--WHERE Id=@LinkChangeActionId
	
	SELECT *
	FROM LINK_ARTIFACT_LINK
	WHERE Id = @ArtifactLinkId
RETURN 0;