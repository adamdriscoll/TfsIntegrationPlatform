CREATE TABLE [dbo].[SERVER_DIFF_RESULT_DETAIL]
(
	Id bigint identity(1,1) primary key NOT NULL,
	ServerDiffResultId bigint NOT NULL,
	DiffDescription nvarchar(max) NOT NULL
)
