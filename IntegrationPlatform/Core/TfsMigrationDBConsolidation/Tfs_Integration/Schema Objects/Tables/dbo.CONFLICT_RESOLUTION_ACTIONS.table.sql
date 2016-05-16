CREATE TABLE [dbo].[CONFLICT_RESOLUTION_ACTIONS]
(
	Id int identity(1,1) NOT NULL primary key, 
	ReferenceName uniqueidentifier NOT NULL,
	FriendlyName nvarchar(300) NOT NULL,
	IsActive bit NULL, -- an actiion may be deactivate if the registering provider no longer supports it
	ProviderId int NULL,
);
