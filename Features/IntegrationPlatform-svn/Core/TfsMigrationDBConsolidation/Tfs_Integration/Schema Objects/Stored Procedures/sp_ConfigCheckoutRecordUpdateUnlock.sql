--IF EXISTS (SELECT * FROM sysobjects WHERE name = 'ConfigCheckoutRecordUpdateUnlock' AND user_name(uid) = 'dbo')
	--DROP PROCEDURE dbo.ConfigCheckoutRecordUpdateUnlock
--GO

CREATE PROCEDURE dbo.ConfigCheckoutRecordUpdateUnlock
(
	@SessionGroupConfigId int
)
AS
	SET NOCOUNT OFF;
UPDATE       CONFIG_CHECKOUT_RECORDS
SET                CheckOutToken = NULL
WHERE        (SessionGroupConfigId = @SessionGroupConfigId );
	 
SELECT SessionGroupConfigId, CheckOutToken FROM CONFIG_CHECKOUT_RECORDS WHERE (SessionGroupConfigId = @SessionGroupConfigId)
GO

