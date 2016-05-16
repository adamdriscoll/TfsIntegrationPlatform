CREATE VIEW [dbo].[SessionGroupDetailsView]
	AS 
SELECT sData.SessionGroupUniqueId AS SessionGroupUniqueId
      ,sData.SessionGroupFriendlyName AS SessionGroupFriendlyName
      ,sData.SessionUniqueId AS SessionUniqueId
      ,sData.SessionFriendlyName AS SessionFriendlyName
      ,sData.SessionType As SessionType
      ,mData.MigrationSourceId AS MigrationSourceId
      ,mData.MigrationSourceUniqueId AS MigrationSourceUniqueId
      ,mData.MigrationSourceFriendlyName As MigrationSourceFriendlyName
FROM
(SELECT SessionData.SessionGroupUniqueId AS SessionGroupUniqueId
       ,SessionData.SessionGroupFriendlyName AS SessionGroupFriendlyName
       ,SessionData.SessionUniqueId AS SessionUniqueId
       ,sc.FriendlyName AS SessionFriendlyName
       ,sc.Type As SessionType
 FROM
  (SELECT sg.[GroupUniqueId] AS SessionGroupUniqueId
      ,sg.[FriendlyName] AS SessionGroupFriendlyName
      ,s.SessionUniqueId AS SessionUniqueId
  FROM [dbo].[SESSION_GROUPS] AS sg
  INNER JOIN [dbo].[RUNTIME_SESSIONS] AS s ON sg.Id = s.SessionGroupId
  GROUP BY sg.[GroupUniqueId]
      ,sg.[FriendlyName]
      ,s.SessionUniqueId) AS SessionData
INNER JOIN [dbo].[SESSION_CONFIGURATIONS] AS sc ON sc.SessionUniqueId = SessionData.SessionUniqueId) As sData
INNER JOIN
(SELECT s.SessionUniqueId AS SessionUniqueId      
      ,ms.Id AS MigrationSourceId
      ,ms.UniqueId AS MigrationSourceUniqueId
      ,ms.FriendlyName As MigrationSourceFriendlyName
  FROM [dbo].[RUNTIME_SESSIONS] AS s
  INNER JOIN [dbo].[MIGRATION_SOURCES] AS ms ON (ms.Id = s.LeftSourceId OR ms.Id = s.RightSourceId)
  GROUP BY s.SessionUniqueId
      ,ms.Id
      ,ms.UniqueId
      ,ms.FriendlyName) AS mData
ON sData.SessionUniqueId = mData.SessionUniqueId
GROUP BY sData.SessionGroupUniqueId
      ,sData.SessionGroupFriendlyName
      ,sData.SessionUniqueId
      ,sData.SessionFriendlyName
      ,sData.SessionType
      ,mData.MigrationSourceId
      ,mData.MigrationSourceUniqueId
      ,mData.MigrationSourceFriendlyName