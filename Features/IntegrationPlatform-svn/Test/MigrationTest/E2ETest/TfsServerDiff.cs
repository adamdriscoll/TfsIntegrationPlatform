// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace ServerDiff 
{
    public class TfsServerDiff
    {
        private VCDiffComparer m_diffComparer;
        private ServerDiffEngine m_serverDiffEngine;
        private VersionControlServer m_targetClient;
        private VersionControlServer m_sourceClient;

        private List<Changeset> m_targetHistory;
        private List<Changeset> m_sourceHistory;

        private bool m_changeToAddOnBranchSourceNotFound;
        private string m_skipChangeComment = string.Empty;

        /// <summary>
        /// The changes in the target system
        /// </summary>
        public List<Changeset> TargetChanges
        {
            get { return m_targetHistory; }
        }

        /// <summary>
        /// The changes in the source system
        /// </summary>
        public List<Changeset> SourceChanges
        {
            get { return m_sourceHistory; }
        }

        /// <summary>
        /// TfsServerDiff will verify the first VC session in the given configuration file
        /// </summary>
        /// <param name="configFileName"></param>
        public TfsServerDiff(Guid sessionGuid, bool addOnBranchSourceNotFound, bool verbose)
        {
            m_serverDiffEngine = new ServerDiffEngine(sessionGuid, false, verbose, SessionTypeEnum.VersionControl);
            m_diffComparer = new VCDiffComparer(m_serverDiffEngine);
            m_serverDiffEngine.RegisterDiffComparer(m_diffComparer);
            m_targetHistory = new List<Changeset>();
            m_sourceHistory = new List<Changeset>();

            var sources = m_serverDiffEngine.Config.SessionGroup.MigrationSources.MigrationSource;
            m_sourceClient = GetTfsClient(sources[0].ServerUrl);
            m_targetClient = GetTfsClient(sources[1].ServerUrl);

            m_changeToAddOnBranchSourceNotFound = addOnBranchSourceNotFound;

            // TODO:
            //m_session.TryGetValue("SkipChangeComment", out m_skipChangeComment);
            Trace.WriteLine("[TODO] Skipped setting SkipChangeComment");
        }

        public bool VerifyContentMatchAtLatest()
        {
            return m_diffComparer.VerifyContentsMatch(null, null);
        }

        /// <summary>
        /// Queries the histories of the servers to builds the targetChanges and sourceChanges objects
        /// </summary>
        public void QueryHistory()
        {
            // TODO: Handle two or more VC sessions
            Debug.Assert(m_serverDiffEngine.Config.SessionGroup.Sessions.Session.Count == 1);
            Debug.Assert(m_serverDiffEngine.Config.SessionGroup.Sessions.Session[0].SessionType == SessionTypeEnum.VersionControl);

            foreach (var filterPair in m_serverDiffEngine.Session.Filters.FilterPair)
            {
                if (!filterPair.Neglect)
                {
                    string sourcePath = GetSourcePath(filterPair);
                    string targetPath = GetTargetPath(filterPair);
                    BuildHistory(m_sourceClient, sourcePath, m_sourceHistory);
                    BuildHistory(m_targetClient, targetPath, m_targetHistory);
                }
            }
        }

        /// <summary>
        /// This method writes all the items in the DependantChanges and targetChanges to the console
        /// </summary>
        /// <param name="diff">The Tfs ServerDiff object to log</param>
        public static void LogFailures(TfsServerDiff diff)
        {
            if (diff.SourceChanges.Count != 0 || diff.TargetChanges.Count != 0)
            {
                Console.WriteLine("Migration had errors");

                if (diff.TargetChanges.Count > 0)
                {
                    Trace.WriteLine("Target Changes Remaining");
                    LogRemaining(diff.TargetChanges);
                }

                if (diff.SourceChanges.Count > 0)
                {
                    Trace.WriteLine(string.Empty);
                    Trace.WriteLine("Source Changes Remaining");
                    LogRemaining(diff.SourceChanges);
                }
            }
            else
            {
                Console.WriteLine("TfsServerDiff: Migration appears to be successful!");
            }
        }

        /// <summary>
        /// Writes a list of changeSets to the standard out. 
        /// </summary>
        /// <param name="list">List of changesets</param>
        private static void LogRemaining(List<Changeset> list)
        {
            foreach (Changeset changeSet in list)
            {
                Trace.WriteLine(string.Format("Id: {0}", changeSet.ChangesetId));
                Trace.WriteLine(string.Format("Comment: {0}", changeSet.Comment));
                Trace.WriteLine(string.Format("Owner: {0}", changeSet.Owner));

                foreach (Change change in changeSet.Changes)
                {
                    Trace.WriteLine(string.Empty);
                    Trace.WriteLine(string.Format("ChangeType: {0}", change.ChangeType));
                    Trace.WriteLine(string.Format("Path: {0}", change.Item.ServerItem));
                }
                Trace.WriteLine(string.Empty);
                Trace.WriteLine(string.Empty);
            }
        }

        /// <summary>
        /// Returns VersionControlServer service
        /// </summary>
        public VersionControlServer GetTfsClient(string serverUrl)
        {
            TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(serverUrl);
            return (VersionControlServer)tfs.GetService(typeof(VersionControlServer));
        }

        /// <summary>
        /// Given a path, find the mapped path according to session mappings
        /// </summary>
        /// <param name="path">the path to be searched</param>
        /// <param name="session">the session</param>
        /// <param name="useTarget">Given path is source path if this value is set to true</param>
        /// <returns></returns>
        public string FindMappedPath(string path, Session session, bool useSource)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            try
            {
                string sourcePath = null;
                string targetPath = null;

                foreach (var filterPair in m_serverDiffEngine.Session.Filters.FilterPair)
                {
                    if (!filterPair.Neglect)
                    {
                        sourcePath = GetSourcePath(filterPair);
                        targetPath = GetTargetPath(filterPair);
                        if (useSource)
                        {
                            if (VersionControlPath.IsSubItem(VersionControlPath.GetFullPath(path), VersionControlPath.GetFullPath(sourcePath)))
                            {
                                return VersionControlPath.Combine(VersionControlPath.GetFullPath(targetPath),
                                    VersionControlPath.GetFullPath(path).Substring(VersionControlPath.GetFullPath(sourcePath).Length).TrimStart(VersionControlPath.Separator));
                            }
                        }
                        else
                        {
                            if (VersionControlPath.IsSubItem(VersionControlPath.GetFullPath(path), VersionControlPath.GetFullPath(targetPath)))
                            {
                                return VersionControlPath.Combine(VersionControlPath.GetFullPath(sourcePath),
                                    VersionControlPath.GetFullPath(path).Substring(VersionControlPath.GetFullPath(targetPath).Length).TrimStart(VersionControlPath.Separator));
                            }
                        }
                    }
                }
            }
            catch (VersionControlException e)
            {
                // This is usually a InvalidPath exception.
                Trace.TraceError(e.Message);
            }
            return null;
        }

        /// <summary>
        /// Compares the contents of targetChanges to sourceChanges and removes any changes that match; or cannot be migrated
        /// </summary>
        public void RemoveSimilarHistory()
        {
            m_targetHistory = new List<Changeset>(m_targetHistory);
            m_sourceHistory = new List<Changeset>(m_sourceHistory);

            //Skipped changes cannot be migrated
            m_targetHistory.RemoveAll(IsSkipped);
            m_sourceHistory.RemoveAll(IsSkipped);

            RemoveCloakedItems(m_targetHistory, IsTargetCloaked);
            RemoveCloakedItems(m_sourceHistory, IsSourceCloaked);


            foreach (Changeset targetChangeset in m_targetHistory.ToArray())
            {
                foreach (Changeset sourceChangeset in m_sourceHistory.ToArray())
                {
                    //Compare the comments to determine if one changeset is potiential the migrated version of another
                    if (CommentsMatch(targetChangeset.Comment, sourceChangeset.Comment))
                    {
                        //Confirm that the changeset was sucessfully migrated by comparing all the changes in the changeset.
                        if (Compare(targetChangeset, sourceChangeset))
                        {
                            //Remove the change set if it is a match
                            m_targetHistory.Remove(targetChangeset);
                            m_sourceHistory.Remove(sourceChangeset);
                            break;
                        }
                    }
                }
            }

            foreach (Changeset targetChangeset in m_targetHistory.ToArray())
            {
                if (branchDeleteOnly(targetChangeset))
                {
                    m_targetHistory.Remove(targetChangeset);
                }
            }

            foreach (Changeset sourceChangeset in m_sourceHistory.ToArray())
            {
                if (branchDeleteOnly(sourceChangeset))
                {
                    m_sourceHistory.Remove(sourceChangeset);
                }
            }
        }

        /// <summary>
        /// Compares two changesets by comparing each of the changes in the changeset
        /// </summary>
        /// <param name="targetChangeset">The changeset on the target system</param>
        /// <param name="sourceChangeset">The changeset on the source system</param>
        public void ChangesetDiff(ref Changeset targetChangeset, ref Changeset sourceChangeset)
        {
            List<Change> targetChanges = new List<Change>(targetChangeset.Changes);
            List<Change> sourceChanges = new List<Change>(sourceChangeset.Changes);

            //Some changes in the source system may not be mapped for migration; remove these
            targetChanges.RemoveAll(IsNotMapped);
            sourceChanges.RemoveAll(IsNotMapped);

            foreach (Change targetChange in targetChanges.ToArray())
            {
                foreach (Change sourceChange in sourceChanges.ToArray())
                {
                    //Compare the changes in a changeset by comparing the change type, server path, and file contents
                    if ((ChangeTypesMatch(targetChange.ChangeType, sourceChange.ChangeType))
                        && (NamesMatch(targetChange.Item.ServerItem, sourceChange.Item.ServerItem))
                        && (ContentsMatch(targetChange.Item.HashValue, sourceChange.Item.HashValue)))
                    {
                        //Remove any changes from the list that matched in every way
                        targetChanges.Remove(targetChange);
                        sourceChanges.Remove(sourceChange);
                        break;
                    }
                }
            }

            // Ignore branch|delete or branch|merge|delete that won't be migrated.
            foreach (Change sourceChange in sourceChanges.ToArray())
            {
                if ((sourceChange.ChangeType & (ChangeType.Branch | ChangeType.Delete)) == (ChangeType.Branch | ChangeType.Delete))
                {
                    sourceChanges.Remove(sourceChange);
                }
            }

            foreach (Change targetChange in targetChanges.ToArray())
            {
                if ((targetChange.ChangeType & (ChangeType.Branch | ChangeType.Delete)) == (ChangeType.Branch | ChangeType.Delete))
                {
                    targetChanges.Remove(targetChange);
                }
            }

            targetChangeset.Changes = targetChanges.ToArray();
            sourceChangeset.Changes = sourceChanges.ToArray();
        }

        #region Private methods
        /// <summary>
        /// Check to see if an item is a sub item in cloak list.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cloakList"></param>
        /// <returns></returns>
        private bool isCloaked(string item, List<string> cloakList)
        {
            foreach (string cloakedPath in cloakList)
            {
                if (VersionControlPath.IsSubItem(item, cloakedPath))
                {
                    return true;
                }
            }
            return false;              
        }
        
        /// <summary>
        /// Adds the changesets of a server path to a List
        /// </summary>
        /// <param name="server">The server the path exists on</param>
        /// <param name="path">The server path to query history for</param>
        /// <param name="history">The list of changes to add to</param>
        private void BuildHistory(VersionControlServer server, string path, List<Changeset> history)
        {
            IEnumerable queryResult = server.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full, null, null, null, int.MaxValue, true, true, false);

            foreach (Changeset changeSet in queryResult)
            {
                if (!history.Contains(changeSet))
                {
                    history.Add(changeSet);
                }
            }
        }

        /// <summary>
        /// Check to see whether the changeset contains only Branch|Delete changes. 
        /// </summary>
        /// <param name="changeset"></param>
        /// <returns></returns>
        private bool branchDeleteOnly(Changeset changeset)
        {
            foreach (Change change in changeset.Changes)
            {
                if ((change.ChangeType & (ChangeType.Branch | ChangeType.Delete)) != (ChangeType.Branch | ChangeType.Delete))
                {
                    return false;
                }
            }
            return true;
        }

        private void RemoveCloakedItems(List<Changeset> history, RemoveCloaked destination)
        {
            foreach (Changeset changeset in history.ToArray())
            {
                List<Change> changeList = new List<Change>(changeset.Changes);

                foreach (Change change in changeset.Changes)
                {
                    string serverPath = change.Item.ServerItem;

                    if (destination(serverPath))
                    {
                        changeList.Remove(change);
                    }
                }

                changeset.Changes = changeList.ToArray();

                if (changeList.Count == 0)
                {
                    history.Remove(changeset);
                }
            }
        }

        private delegate bool RemoveCloaked(string serverPath);

        private bool IsTargetCloaked(string serverPath)
        {
            foreach (var filterPair in m_serverDiffEngine.Session.Filters.FilterPair)
            {
                if (filterPair.Neglect)
                {
                    string targetPath = filterPair.FilterItem[1].FilterString;

                    if (VersionControlPath.IsSubItem(serverPath, targetPath))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsSourceCloaked(string serverPath)
        {
            foreach (var filterPair in m_serverDiffEngine.Session.Filters.FilterPair)
            {
                if (filterPair.Neglect)
                {
                    string sourcePath = GetSourcePath(filterPair);

                    if (VersionControlPath.IsSubItem(serverPath, sourcePath))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// A changeset can be skipped because it has the SkipChangeComment comment
        /// </summary>
        /// <param name="change">Changeset that may have been skipped</param>
        /// <returns>True if the changeset Comment contains the SkipChangeComment; false otherwise</returns>
        private bool IsSkipped(Changeset change)
        {
            if (string.IsNullOrEmpty(change.Comment) || string.IsNullOrEmpty(m_skipChangeComment))
            {
                return false;
            }
            if (change.Comment.Contains(m_skipChangeComment))
            {
                return true;
            }
            return false;
        }

        private static bool OwnersMatch(string targetOwner, string sourceOwner)
        {
            // TODO:
            Trace.WriteLine("[TODO] Skipping Owners Match");
            return true;

            //if (targetOwner.Equals(sourceOwner))
            //{
            //    return true;
            //}

            //if (ConfigurationManager.AppSettings[targetOwner] != null)
            //    if (ConfigurationManager.AppSettings[targetOwner].Equals(sourceOwner, StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        return true;
            //    }
            //    else
            //    {
            //        Trace.WriteLine(string.Format(
            //            "TargetOwner \"{0}\" matched a user mapping, but the SourceOwner: \"{1}\" did not match the user mapped to: \"{2}\" Verify that the mapping is correct.",
            //            targetOwner, sourceOwner, ConfigurationManager.AppSettings[targetOwner]));
            //    }

            //if (ConfigurationManager.AppSettings[sourceOwner] != null)
            //    if (ConfigurationManager.AppSettings[sourceOwner].Equals(targetOwner, StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        return true;
            //    }
            //    else
            //    {
            //        Trace.WriteLine(string.Format(
            //            "SourceOwner \"{0}\" matched a user mapping, but the TargetOwner: \"{1}\" did not match the user mapped to: \"{2}\" Verify that the mapping is correct.",
            //            sourceOwner, targetOwner, ConfigurationManager.AppSettings[sourceOwner]));
            //    }

            //return false;
        }

        /// <summary>
        /// Compares two changesets by comparing thier users, the comparing each of the changes in the changeset
        /// </summary>
        /// <param name="targetChangeset">The changeset on the target system</param>
        /// <param name="sourceChangeset">The changeset on the source system</param>
        /// <returns>true if one changes set is the migrated version of the other; false otherwise</returns>
        private bool Compare(Changeset targetChangeset, Changeset sourceChangeset)
        {
            if (!OwnersMatch(targetChangeset.Owner, sourceChangeset.Owner))
            {
                return false;
            }

            List<Change> targetChanges = new List<Change>(targetChangeset.Changes);
            List<Change> sourceChanges = new List<Change>(sourceChangeset.Changes);

            //Some changes in the source system may not be mapped for migration; remove these
            targetChanges.RemoveAll(IsNotMapped);
            sourceChanges.RemoveAll(IsNotMapped);

            foreach (Change targetChange in targetChanges.ToArray())
            {
                foreach (Change sourceChange in sourceChanges.ToArray())
                {
                    //Compare the changes in a changeset by comparing the change type, server path, and file contents
                    if ((ChangeTypesMatch(targetChange.ChangeType, sourceChange.ChangeType))
                        && (NamesMatch(targetChange.Item.ServerItem, sourceChange.Item.ServerItem))
                        && (ContentsMatch(targetChange.Item.HashValue, sourceChange.Item.HashValue)))
                    {
                        //Remove any changes from the list that matched in every way
                        targetChanges.Remove(targetChange);
                        sourceChanges.Remove(sourceChange);
                        break;
                    }
                }
            }

            // Ignore branch|delete or branch|merge|delete that won't be migrated.
            foreach (Change sourceChange in sourceChanges.ToArray())
            {
                if ((sourceChange.ChangeType & (ChangeType.Branch | ChangeType.Delete)) == (ChangeType.Branch | ChangeType.Delete))
                {
                    sourceChanges.Remove(sourceChange);
                }
            }

            foreach (Change targetChange in targetChanges.ToArray())
            {
                if ((targetChange.ChangeType & (ChangeType.Branch | ChangeType.Delete)) == (ChangeType.Branch | ChangeType.Delete))
                {
                    targetChanges.Remove(targetChange);
                }
            }

            // Every change was removed from both lists then these changesets matched
            if (targetChanges.Count == 0 && sourceChanges.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compare two change sets within the limits for migration
        /// </summary>
        /// <param name="targetChangeType">ChangeType on item for one system</param>
        /// <param name="dependantChangeType">ChangeType for item in other system</param>
        /// <returns>true if the changetypes are equivelent when ignoring the bits that cannot be migrated</returns>
        private bool ChangeTypesMatch(ChangeType targetChangeType, ChangeType sourceChangeType)
        {
            //Toolkit ignores encoding
            targetChangeType = targetChangeType & ~ChangeType.Encoding;
            sourceChangeType = sourceChangeType & ~ChangeType.Encoding;

            if ((sourceChangeType | (ChangeType.Merge | ChangeType.Undelete)) == sourceChangeType)
            {
                sourceChangeType = sourceChangeType & ~ChangeType.Merge;
            }

            RemoveAddEdit(ref targetChangeType, ref sourceChangeType);

            if (targetChangeType == sourceChangeType)
            {
                return true;
            }

            // With the merge performance change, we migrate branch as merge, this will create a Branch|Merge for us for a source Branch.
            if (((targetChangeType & ChangeType.Branch) != 0) && (targetChangeType == (sourceChangeType | ChangeType.Merge)))
            {
                return true;
            }

            //Merge | Delete is migrated as a Delete (see: OrcasBug 297264)
            if (CheckChangeTypeSpecialCase(targetChangeType, sourceChangeType, (ChangeType.Merge | ChangeType.Delete), ChangeType.Delete))
            {
                return true;
            }

            //Baseless merge is migrated as a branch
            if (CheckChangeTypeSpecialCase(targetChangeType, sourceChangeType, (ChangeType.Branch | ChangeType.Merge), ChangeType.Branch))
            {
                return true;
            }

            //Toolkit ignores Delete | Undelete
            if (CheckChangeTypeSpecialCase(targetChangeType, sourceChangeType, (ChangeType.Delete | ChangeType.Undelete), ChangeType.Undelete))
            {
                return true;
            }

            return IsMigratedWithoutIntegrationHistory(targetChangeType, sourceChangeType);
        }

        private static void RemoveAddEdit(ref ChangeType targetChangeType, ref ChangeType sourceChangeType)
        {
            if (targetChangeType == (ChangeType.Add | ChangeType.Edit))
            {
                targetChangeType = targetChangeType & ~ChangeType.Edit;
            }

            if (sourceChangeType == (ChangeType.Add | ChangeType.Edit))
            {
                sourceChangeType = sourceChangeType & ~ChangeType.Edit;
            }
        }

        private bool IsMigratedWithoutIntegrationHistory(ChangeType targetChangeType, ChangeType sourceChangeType)
        {
            if (m_changeToAddOnBranchSourceNotFound)
            {
                if (targetChangeType == ChangeType.Add && sourceChangeType == (ChangeType.Branch | ChangeType.Merge))
                {
                    return true;
                }

                if (targetChangeType == ChangeType.Add && sourceChangeType == ChangeType.Branch)
                {
                    return true;
                }

                if (sourceChangeType == ChangeType.Add && targetChangeType == ChangeType.Branch)
                {
                    return true;
                }

                if ((targetChangeType & ~ChangeType.Merge) == sourceChangeType)
                {
                    return true;
                }

                if ((sourceChangeType & ~ChangeType.Merge) == targetChangeType)
                {
                    return true;
                }

                if ((sourceChangeType & ~ChangeType.Rename & ~ChangeType.Edit) == (targetChangeType & ~ChangeType.Add))
                {
                    return true;
                }

                if ((targetChangeType & ~ChangeType.Rename & ~ChangeType.Edit) == (sourceChangeType & ~ChangeType.Add))
                {
                    return true;
                }

                if (((sourceChangeType & (ChangeType.Merge | ChangeType.Branch)) == (ChangeType.Merge | ChangeType.Branch))
                    && ((targetChangeType & ChangeType.Add) == ChangeType.Add))
                {
                    // Branch|Merge|Delete shouldn't be migrated as Add
                    if ((sourceChangeType & ChangeType.Delete) == ChangeType.Delete)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                if (((targetChangeType & (ChangeType.Merge | ChangeType.Branch)) == (ChangeType.Merge | ChangeType.Branch))
                    && ((sourceChangeType & ChangeType.Add) == ChangeType.Add))
                {
                    // Branch|Merge|Delete shouldn't be migrated as Add
                    if ((sourceChangeType & ChangeType.Delete) == ChangeType.Delete)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Helpler method for comparing two change types where some information is lost
        /// </summary>
        /// <param name="targetChangeType">change type from one changeset</param>
        /// <param name="dependantChangeType">change type form other changeset</param>
        /// <param name="specialCase">The special changeType to look for</param>
        /// <param name="result">The changetype the specialCase will be migrated as</param>
        /// <returns>Ture if one changetype  matches the special case and the other mathces the result; false otherwise</returns>
        private bool CheckChangeTypeSpecialCase(ChangeType targetChangeType, ChangeType sourceChangeType, ChangeType specialCase, ChangeType result)
        {
            if (targetChangeType == specialCase)
            {
                if (sourceChangeType == result)
                {
                    return true;
                }
            }

            if (sourceChangeType == specialCase)
            {
                if (targetChangeType == result)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determine if the item in this change is included in the migration mappings
        /// </summary>
        /// <param name="change">Change to check the mappings for</param>
        /// <returns>true if this change was not mapped; false if the change was mapped</returns>
        private bool IsNotMapped(Change change)
        {
            if (FindMappedPath(change.Item.ServerItem, m_serverDiffEngine.Session, false) != null
                || FindMappedPath(change.Item.ServerItem, m_serverDiffEngine.Session, true) != null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///  Compare the hash values for two files in the tfs servers
        /// </summary>
        /// <param name="targetMd5Sum">One files hashvalue</param>
        /// <param name="sourceMd5Sum">The other files hash value</param>
        /// <returns>true if the hash values are the same for both files</returns>
        private static bool ContentsMatch(byte[] targetMd5Sum, byte[] sourceMd5Sum)
        {
            if (targetMd5Sum.Length != sourceMd5Sum.Length)
            {
                return false;
            }

            for (int i = 0; i < targetMd5Sum.Length; i++)
            {
                if (targetMd5Sum[i] != sourceMd5Sum[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compare the server paths of two files in tfs with respect to the migration mappings. 
        /// </summary>
        /// <param name="targetName">One server path</param>
        /// <param name="sourceName">The other server path</param>
        /// <returns>true if according to the migration mapping one server path would result in the other</returns>
        private bool NamesMatch(string targetName, string sourceName)
        {
            if (FindMappedPath(targetName, m_serverDiffEngine.Session, false).Equals(sourceName, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Compare the comments of two changeset with respect to the note that the migration toolkit appends to changes it migrates
        /// </summary>
        /// <param name="targetComment">the comment of one changeset</param>
        /// <param name="sourceComment">the comment of the other changeset</param>
        /// <returns>true if migrating one changeset comment would result in the other changeset comment</returns>
        private bool CommentsMatch(string targetComment, string sourceComment)
        {
            //A null comment is equavilent to an empty string but impossible to compare
            if (targetComment == null)
            {
                targetComment = string.Empty;
            }

            if (sourceComment == null)
            {
                sourceComment = string.Empty;
            }

            string format = "{0} (TFS Integration";

            string migratedFromTFS = String.Format(format, sourceComment);

            if (targetComment.StartsWith(migratedFromTFS))
            {
                return true;
            }

            return false;
        }
              
        private string GetSourcePath(FilterPair filterPair)
        {
            if (Guid.Equals(new Guid(m_serverDiffEngine.Session.LeftMigrationSourceUniqueId), new Guid(filterPair.FilterItem[0].MigrationSourceUniqueId)))
            {
                return filterPair.FilterItem[0].FilterString;
            }
            else
            {
                return filterPair.FilterItem[1].FilterString;
            }
        }

        private string GetTargetPath(FilterPair filterPair)
        {
            if (Guid.Equals(new Guid(m_serverDiffEngine.Session.LeftMigrationSourceUniqueId), new Guid(filterPair.FilterItem[0].MigrationSourceUniqueId)))
            {
                return filterPair.FilterItem[1].FilterString;
            }
            else
            {
                return filterPair.FilterItem[0].FilterString;
            }
        }
        #endregion
    }
}