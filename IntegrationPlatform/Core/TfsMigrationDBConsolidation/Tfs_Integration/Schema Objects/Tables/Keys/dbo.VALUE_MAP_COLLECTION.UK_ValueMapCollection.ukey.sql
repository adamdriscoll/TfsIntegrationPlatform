ALTER TABLE [dbo].[VALUE_MAP_COLLECTION]
ADD CONSTRAINT [UK_ValueMapCollection]
UNIQUE (MappingId, FieldMapCollectionId)