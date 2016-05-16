ALTER TABLE [dbo].[RUNTIME_GENERAL_PERFORMANCE_DATA]
ADD CONSTRAINT [UK_RT_PerfData]
UNIQUE (SessionGroupRunId, SessionUniqueId, SourceUniqueId, CriterionReferenceName)