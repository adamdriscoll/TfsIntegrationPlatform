// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFSIntegrationAdmin.CompressCmd
{
    static class SqlScripts
    {
        public const string BatchDeleteProcessedMigrationInstruction =
@"
DECLARE @DeletionChangeGroupId TABLE(Id BIGINT)
 DECLARE @DeletionChangeActionId TABLE(Id BIGINT)

 INSERT INTO @DeletionChangeGroupId(Id)
 SELECT TOP 100 Id
 FROM dbo.RUNTIME_CHANGE_GROUPS WITH(NOLOCK)
 WHERE ((Status = 6 OR Status = 10) AND ContainsBackloggedAction = 0)

 -- RETRIEVE FIRST BATCH
 INSERT INTO @DeletionChangeActionId(Id)
 SELECT TOP 1000 a.ChangeActionId
 FROM dbo.RUNTIME_CHANGE_ACTION AS a WITH(NOLOCK)
 INNER JOIN @DeletionChangeGroupId AS g
    ON g.Id = a.ChangeGroupId
 
 -- SKIP DELETION IF THERE ARE NOT *MANY* JUNK IN DB
 WHILE (SELECT COUNT(*) FROM @DeletionChangeActionId) > 0
 BEGIN
  PRINT CONVERT(VARCHAR(12),GETDATE(),114) + N' - Deleting 10000 RUNTIME_CHANGE_ACTION rows'
  DELETE FROM dbo.RUNTIME_CHANGE_ACTION
  WHERE ChangeActionId IN (SELECT Id FROM @DeletionChangeActionId)
  
  -- RETRIEVE NEXT BATCH
  DELETE FROM @DeletionChangeActionId 
  
  INSERT INTO @DeletionChangeActionId(Id)
	 SELECT TOP 1000 a.ChangeActionId
	 FROM dbo.RUNTIME_CHANGE_ACTION AS a WITH(NOLOCK)
	 INNER JOIN @DeletionChangeGroupId AS g
		ON g.Id = a.ChangeGroupId
 END
 
 DELETE FROM dbo.RUNTIME_CHANGE_GROUPS
 WHERE Id IN (SELECT * FROM @DeletionChangeGroupId)

 SELECT COUNT(*)
 FROM dbo.RUNTIME_CHANGE_GROUPS WITH(NOLOCK)
 WHERE ((Status = 6 OR Status = 10) AND ContainsBackloggedAction = 0)
";
    }
}
