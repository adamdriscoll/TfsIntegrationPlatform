﻿ALTER TABLE [dbo].[RUNTIME_MIGRATION_ARTIFACT_LINK_JUNC]
	ADD CONSTRAINT [PK_RT_MigArtifactLinkJunc]
	PRIMARY KEY (LinkMigrationResultId, ArtifactLinkId)