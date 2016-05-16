--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordUpdateLock' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordUpdateLock
--GO

CREATE PROCEDURE dbo.ConfigCheckoutRecordUpdateLock
(
	@CheckOutToken uniqueidentifier,
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT OFF;
UPDATE       CONFIG_CHECKOUT_RECORDS
SET                CheckOutToken = @CheckOutToken
WHERE        (SessionGroupConfigId = @SessionGroupConfigId);
	 
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO

