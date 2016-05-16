CREATE TABLE [dbo].[FIELD_MAP_COLLECTION]
(
	Id int NOT NULL identity(1,1), 
	WITypeMapId int NOT NULL,
	ValueMappingId int NOT NULL,
	Direction int NOT NULL
);
