ALTER TABLE [dbo].[CONFLICT_CONFLICTS]
	ADD CONSTRAINT [FK_Conflicts_to_ChangeAction] 
	FOREIGN KEY (ChangeGroupId, ConflictedChangeActionId)
	REFERENCES RUNTIME_CHANGE_ACTION (ChangeGroupId, ChangeActionId) ON DELETE SET NULL	

