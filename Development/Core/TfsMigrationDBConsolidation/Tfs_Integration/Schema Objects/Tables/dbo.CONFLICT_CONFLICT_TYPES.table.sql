CREATE TABLE [dbo].[CONFLICT_CONFLICT_TYPES]
(
    Id int identity(1,1) NOT NULL,
	ReferenceName uniqueidentifier NOT NULL,
	FriendlyName nvarchar(300) NOT NULL,
	DescriptionDoc nvarchar(max) NULL,
	IsActive bit NULL, -- an actiion may be deactivate if the registering provider no longer supports it
	ProviderId int NULL,
);
