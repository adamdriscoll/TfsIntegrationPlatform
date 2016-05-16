CREATE TABLE [dbo].[RELATED_ARTIFACTS_RECORDS]
(
	Id bigint identity(1,1) NOT NULL primary key, 
	MigrationSourceId int,						-- the migration source, to which the Work Item belongs
	ItemId NVARCHAR(4000) NOT NULL,				-- usu. this column stores host Work Item Id
	Relationship NVARCHAR(1000) NOT NULL,		-- e.g. "Attachment" for TFS attachment
												-- and LinkTypeReferenceNane for TFS links
	RelatedArtifactId NVARCHAR(4000) NOT NULL,	-- e.g. for Work Item Links, this can be linked artifact Url
												-- and for Attachments, this can be attachment file Hash
	RelationshipExistsOnServer bit NOT NULL,	-- when first created by TiP, this bit is set to 1
												-- when the relationship is broken, e.g. attachment or link is deleted
												-- this bit is set to 0
	OtherProperty int NULL						-- for link, this is used to track whether a link is locked
												-- for attachment, this is used to track the count of identical attachment file
)
