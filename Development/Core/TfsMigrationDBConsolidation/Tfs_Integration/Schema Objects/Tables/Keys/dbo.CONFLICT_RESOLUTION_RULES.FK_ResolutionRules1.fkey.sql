﻿ALTER TABLE [dbo].[CONFLICT_RESOLUTION_RULES]
	ADD CONSTRAINT [FK_ResolutionRules1] 
	FOREIGN KEY (ScopeId)
	REFERENCES CONFLICT_RULE_SCOPES (Id)	
