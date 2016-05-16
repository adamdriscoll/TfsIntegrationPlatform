--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordInsert' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordInsert
--GO

CREATE PROCEDURE dbo.ConfigCheckoutRecordInsert
(
	@SessionGroupConfigId int,
	@CheckOutToken uniqueidentifier
)
AS
	SET NOCOUNT OFF;
INSERT INTO [dbo].[CONFIG_CHECKOUT_RECORDS] ([SessionGroupConfigId], [CheckOutToken]) VALUES (@SessionGroupConfigId, @CheckOutToken);
	
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO

