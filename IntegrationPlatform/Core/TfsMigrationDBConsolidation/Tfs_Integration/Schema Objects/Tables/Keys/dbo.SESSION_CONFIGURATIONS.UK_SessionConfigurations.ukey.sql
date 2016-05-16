ALTER TABLE [dbo].[SESSION_CONFIGURATIONS]
ADD CONSTRAINT [UK_SessionConfigurations]
UNIQUE (SessionUniqueId, SessionGroupConfigId, CreationTime, LeftSourceConfigId, RightSourceConfigId, Type)