﻿ALTER TABLE [dbo].[RUNTIME_REGISTERED_ACTIONS]
ADD CONSTRAINT [UK_RT_RegisteredActions]
UNIQUE (ReferenceName)