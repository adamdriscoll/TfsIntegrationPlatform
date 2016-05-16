CREATE TABLE [dbo].[SERVER_DIFF_RESULT]
(
	Id bigint identity(1,1) primary key NOT NULL,
	DiffType nvarchar(50) NOT NULL,
	DiffTime datetime NOT NULL,
	DurationOfDiff int NOT NULL,
	SessionUniqueId uniqueidentifier NOT NULL,
	AllContentsMatch bit NOT NULL,
	Options nvarchar(max) NOT NULL
)
