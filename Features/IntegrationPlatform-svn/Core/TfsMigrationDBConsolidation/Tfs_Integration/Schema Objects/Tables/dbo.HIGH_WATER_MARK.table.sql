CREATE TABLE [dbo].[HIGH_WATER_MARK]
(
	Id int NOT NULL identity(1,1), 
	SessionUniqueId uniqueidentifier NOT NULL,
	SourceUniqueId uniqueidentifier NOT NULL,
	Name nvarchar(50) NOT NULL,
	Value nvarchar(50)
);
