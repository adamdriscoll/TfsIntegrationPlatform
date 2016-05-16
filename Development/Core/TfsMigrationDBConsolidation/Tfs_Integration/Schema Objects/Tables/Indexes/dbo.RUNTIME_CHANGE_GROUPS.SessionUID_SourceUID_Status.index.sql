CREATE INDEX [SessionUID_SourceUID_Status]
    ON [dbo].[RUNTIME_CHANGE_GROUPS]
	(SessionUniqueId, SourceUniqueId, Status)


