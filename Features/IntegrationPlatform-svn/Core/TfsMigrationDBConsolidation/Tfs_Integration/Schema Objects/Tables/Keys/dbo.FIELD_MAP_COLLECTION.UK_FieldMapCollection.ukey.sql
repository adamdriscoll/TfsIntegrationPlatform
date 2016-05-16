ALTER TABLE [dbo].[FIELD_MAP_COLLECTION]
ADD CONSTRAINT [UK_FieldMapCollection]
UNIQUE (WITypeMapId, ValueMappingId, Direction)