CREATE TABLE [dbo].[SYNC_POINT]
(
	Id bigint identity(1,1) NOT NULL, 
	SessionUniqueId uniqueidentifier NOT NULL,
	SourceUniqueId uniqueidentifier NOT NULL,
	SourceHighWaterMarkName nvarchar(50) NOT NULL,
	SourceHighWaterMarkValue nvarchar(50),
	LastMigratedTargetItemId NVARCHAR(300) NOT NULL,
	LastMigratedTargetItemVersion NVARCHAR(50) NOT NULL,
	LastChangeGroupId BIGINT
)
