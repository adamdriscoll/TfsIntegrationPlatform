CREATE PROCEDURE prc_UpdateSessionVariable
	@SessionId nvarchar(max),
	@Variable nvarchar(256),
	@Value nvarchar(max)
AS

IF EXISTS(
	SELECT TOP 1 * FROM Old_SessionState
	WHERE	SessionId=@SessionId
			AND Variable=@Variable
	)
	BEGIN
		UPDATE Old_SessionState
		SET Value=@Value
		WHERE	SessionId=@SessionId
				AND Variable=@Variable 
	END
	ELSE
	BEGIN
		INSERT INTO Old_SessionState VALUES(@SessionId, @Variable, @Value)
	END