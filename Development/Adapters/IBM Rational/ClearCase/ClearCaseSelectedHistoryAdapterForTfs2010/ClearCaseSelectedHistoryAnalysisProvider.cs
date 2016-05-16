using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter;
using ClearCase;
using Microsoft.TeamFoundation.Migration.Toolkit;

using Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter;

namespace Microsoft.TeamFoundation.Migration.ClearCaseSelectedHistoryAdapter
{
    class ClearCaseSelectedHistoryAnalysisProvider : TfsFileSystemAnalysisProvider
    {
        CCConfiguration m_ccConfiguration;
        ClearCaseServer m_clearCaseServer;

        public ClearCaseSelectedHistoryAnalysisProvider(bool syncToLastTfsChangeset)
            : base(syncToLastTfsChangeset)
        {
        }
        public override Dictionary<string, string> GetRenameList()
        {
            if (!m_ccConfiguration.QueryRenameHistory)
            {
                return null;
            }
            m_hwmDelta.Reload();
            DateTime since = m_hwmDelta.Value;
            List<ClearCaseDetailedHistoryAdapter.CCHistoryRecord> historyRecordList = m_clearCaseServer.GetHistoryRecords(m_configurationService.Filters, since, true);
            historyRecordList.Sort();

            CCVersion version;
            CCItem currentItem = null;
            CCItem previousLnItem = null;
            string previousLnItemLeafName = null;
            bool isDirectory;
            List<ClearCaseDetailedHistoryAdapter.CCHistoryRecord> processedRecordList = new List<ClearCaseDetailedHistoryAdapter.CCHistoryRecord>();
            Dictionary<string, string> renameList = new Dictionary<string, string>();
            foreach (ClearCaseDetailedHistoryAdapter.CCHistoryRecord historyRecord in historyRecordList)
            {
                switch (historyRecord.OperationType)
                {
                    case OperationType.Lnname:
                        version = m_clearCaseServer.ApplicationClass.get_Version(historyRecord.VersionExtendedPath);
                        currentItem = new CCItem(version, ClearCasePath.GetVobName(historyRecord.AbsoluteVobPath));
                        previousLnItem = currentItem;
                        previousLnItemLeafName = ClearCaseEventSpec.ParseLnNameComment(historyRecord.Comment);
                        break;
                    case OperationType.Rmname:
                        string rmItemName = ClearCaseEventSpec.ParseRmNameComment(historyRecord.Comment, out isDirectory);
                        version = m_clearCaseServer.ApplicationClass.get_Version(historyRecord.VersionExtendedPath);
                        currentItem = new CCItem(version, ClearCasePath.GetVobName(historyRecord.AbsoluteVobPath));
                        if (currentItem.Equals(previousLnItem))
                        {
                            string renameTo = ClearCasePath.Combine(currentItem.AbsoluteVobPath, previousLnItemLeafName);
                            string renameFrom = ClearCasePath.Combine(currentItem.AbsoluteVobPath, rmItemName);
                            addEntryToRenameList(renameList, renameFrom, renameTo);
                            previousLnItem = null;
                            previousLnItemLeafName = null;
                        }
                        break;
                    default:
                        break;
                }
            }
            return renameList;
        }

        private void addEntryToRenameList(Dictionary<string, string> renameList, string renameFrom, string renameTo)
        {
            // Todo, handle cyclic rename
            if (!renameList.ContainsKey(renameFrom))
            {
                bool itemExist = false;
                foreach (KeyValuePair<string, string> existingRenameItem in renameList)
                {
                    if (string.Equals(existingRenameItem.Value, renameFrom))
                    {
                        renameList[existingRenameItem.Key] = renameTo;
                        itemExist = true;
                        break;
                    }
                }
                if (!itemExist)
                {
                    renameList.Add(renameFrom, renameTo);
                }
            }
        }

        public override void InitializeClient()
        {
            initializeConfiguration();
            initializeClearCaseServer();
            undoAllCheckouts();
            if (m_ccConfiguration.UseDynamicView)
            {
                checkViewPrivateFiles();
            }
            base.InitializeClient();
        }

        private void initializeConfiguration()
        {
            m_ccConfiguration = CCConfiguration.GetInstance(m_configurationService.MigrationSource);
        }

        private void initializeClearCaseServer()
        {
            m_clearCaseServer = ClearCaseServer.GetInstance(m_ccConfiguration, m_ccConfiguration.GetViewName("Analysis"));
            m_clearCaseServer.Initialize();
        }

        /// <summary>
        /// Undo all checkouts on this clearcase server
        /// </summary>
        private void undoAllCheckouts()
        {
            foreach (MappingEntry mappingEntry in m_configurationService.Filters)
            {
                m_clearCaseServer.UndoCheckoutRecursive(mappingEntry.Path);
            }
        }

        private void checkViewPrivateFiles()
        {
            List<string> privateViewFiles = m_clearCaseServer.QueryViewPrivateFiles(m_ccConfiguration.PrecreatedViewName);
            
            if ((privateViewFiles != null ) && (privateViewFiles.Count > 0))
            {
                foreach (string privateViewFile in privateViewFiles)
                {
                    TraceManager.TraceWarning(string.Format("View private file - {0}", privateViewFile));
                }            
                throw new MigrationException(string.Format(
                    "There are view private files in view {0}. Please remove these files and restart the migration.", 
                    m_ccConfiguration.PrecreatedViewName));
            }
        }
    }
}
