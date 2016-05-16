// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using ClearCase;

using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// This class wraps calls to the clearcase server.
    /// </summary>
    public class ClearCaseServer
    {
        private static Dictionary<string, ClearCaseServer> instanceTable = new Dictionary<string, ClearCaseServer>();

        ClearToolClass m_clearCaseTool;
        CCView m_clearCaseView;

        // For a branch test, branch name is 'test', BranchVersionString is 'main\test';
        public string BranchVersionString { get; private set; }
        public string BranchName { get; private set; }
        public string ViewName { get; set; }
        public string StorageLocation { get; private set;}
        public string StorageLocationLocalPath { get; private set; }
        public ApplicationClass ApplicationClass { get; private set; }
        public string ViewRootPath { get; private set; }
        public string ViewRootLocalPath { get; private set; }
        public List<string> VobList { get; private set; }
        public bool UsePrecreatedView { get; private set; }
        public bool UseDynamicView { get; private set; }

        #region clearfsimport variables - Properties to record the state of clearfsimport command execution.
        private static Dictionary<string, ClearfsimportResult> s_clearfsimportOutput = new Dictionary<string, ClearfsimportResult>();
        private static List<string> s_clearfsimportErrorOutput = new List<string>();
        private static string  s_elementPath = string.Empty;
        private static ClearfsimportResult s_previousState = ClearfsimportResult.Initialize;
        #endregion 


        public CCView ClearCaseView
        {
            get
            {
                if (m_clearCaseView == null)
                {
                    m_clearCaseView = ApplicationClass.get_View(ViewName);
                }
                return m_clearCaseView;
            }
        }

        /// <summary>
        /// Get an instance of the ClearCaseServer object
        /// </summary>
        /// <param name="storageLocation"></param>
        /// <param name="viewName"></param>
        /// <returns></returns>
        public static ClearCaseServer GetInstance(
            string storageLocation,
            string storageLocationLocalPath,
            string viewName,
            List<string> vobList,
            string branchName)
        {
            if (!instanceTable.ContainsKey(viewName))
            {
                instanceTable.Add(viewName, new ClearCaseServer(storageLocation, storageLocationLocalPath, viewName, vobList, branchName));
            }
            return instanceTable[viewName];
        }

        /// <summary>
        /// Get an instance of the ClearCaseServer object
        /// </summary>
        /// <param name="storageLocation"></param>
        /// <param name="viewName"></param>
        /// <returns></returns>
        public static ClearCaseServer GetInstance(CCConfiguration configuration, string viewName)
        {
            if (!instanceTable.ContainsKey(viewName))
            {
                instanceTable.Add(viewName, new ClearCaseServer(configuration, viewName));
            }   
            return instanceTable[viewName];
        }

        /// <summary>
        /// Get an instance of the ClearCaseServer object
        /// </summary>
        /// <param name="storageLocation"></param>
        /// <param name="viewName"></param>
        /// <returns></returns>
        public static ClearCaseServer GetInstance(string viewName)
        {
            if ((instanceTable == null) || (!instanceTable.ContainsKey(viewName)))
            {
                throw new MigrationException(String.Format(CCResources.ClearCaseServerNotFound, viewName));
            }
            return instanceTable[viewName];
        }

        /// <summary>
        /// 1. Call all ClearCaseServer object in instanceTable to release COM object
        /// 2. Clear the instanceTable
        /// </summary>
        public static void CleanUp()
        {
            if (instanceTable == null)
            {
                return;
            }
            foreach (KeyValuePair<string, ClearCaseServer> instanceTableEntry in instanceTable)
            {
                instanceTableEntry.Value.cleanUp();
            }
            instanceTable.Clear();
        }

        /// <summary>
        /// Get the vob absolute path from a relative path.
        /// </summary>
        /// <param name="relativePath"></param>
        public string GetVobAbsolutePathFromRelativePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return relativePath;
            }
            else
            {
                return ClearCasePath.Combine(ViewRootPath, relativePath);
            }
        }

        /// <summary>
        /// Get the vob absolute path from a relative path.
        /// </summary>
        /// <param name="relativePath"></param>
        public string GetRelativePathFromVobAbsolutePath(string vobAbsolutePath)
        {
            if (string.IsNullOrEmpty(vobAbsolutePath) || !vobAbsolutePath.StartsWith(ViewRootPath))
            {
                return vobAbsolutePath;
            }
            else
            {
                return vobAbsolutePath.Substring(ViewRootPath.Length);
            }
        }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        private ClearCaseServer(CCConfiguration configuration, string viewName)
        {
            ApplicationClass = new ApplicationClass();
            m_clearCaseTool = new ClearToolClass();
            StorageLocation = configuration.StorageLocation;
            StorageLocationLocalPath = configuration.StorageLocationLocalPath;
            ViewName = viewName;
            ViewRootPath = configuration.UseDynamicView ? configuration.DynamicViewRoot : ClearCasePath.Combine(StorageLocation, ViewName);
            ViewRootLocalPath = configuration.UseDynamicView ? configuration.DynamicViewRoot 
                                                             : ClearCasePath.Combine(StorageLocationLocalPath, ViewName);
            BranchName = configuration.BranchName;

            if (configuration.UsePrecreatedView)
            {
                BranchVersionString = string.Empty;
            }
            else if (string.Equals(BranchName, CCResources.DefaultBranchName, StringComparison.OrdinalIgnoreCase))
            {
                BranchVersionString = BranchName;
            }
            else
            {
                BranchVersionString = string.Format(CCResources.BranchVersionString, BranchName);
            }
            VobList = configuration.VobList;
            UsePrecreatedView = configuration.UsePrecreatedView;
            UseDynamicView = configuration.UseDynamicView;
            verifyVobAndBranchType();
        }

        /// <summary>
        /// Create an ClearCaseServer that uses a snapshot view that will be created in runtime.
        /// </summary>
        private ClearCaseServer(string storageLocation, string storageLocationLocalPath, string viewName, List<string> vobList, string branchName)
        {
            ApplicationClass = new ApplicationClass();
            m_clearCaseTool = new ClearToolClass();
            StorageLocation = storageLocation;
            StorageLocationLocalPath = storageLocationLocalPath;
            ViewName = viewName;
            ViewRootPath = ClearCasePath.Combine(StorageLocation, ViewName);
            ViewRootLocalPath = ClearCasePath.Combine(StorageLocationLocalPath, ViewName);
            BranchName = branchName;

            if (string.Equals(branchName, CCResources.DefaultBranchName, StringComparison.OrdinalIgnoreCase))
            {
                BranchVersionString = BranchName;
            }
            else
            {
                BranchVersionString = string.Format(CCResources.BranchVersionString, branchName);
            }
            VobList = vobList;
            UsePrecreatedView = false;
            UseDynamicView = false;
            verifyVobAndBranchType();
        }

        private void verifyVobAndBranchType()
        {
            CCVOB ccVob;
            foreach (string vob in VobList)
            {
                try
                {
                    ccVob = ApplicationClass.get_VOB(vob);
                    if (string.IsNullOrEmpty(BranchName))
                    {
                        // BranchName is empty. This is possible when the user provide a precreated view in the configuration file. 
                        // Skip the logic to verify branches. 
                        continue;
                    }
                }
                catch (COMException)
                {
                    throw new MigrationException("VOB {0} does not exist.", vob);
                }
                try
                {
                    ccVob.get_BranchType(BranchName, false);
                }
                catch(COMException)
                {
                    TraceManager.TraceInformation("Create branch type {0} in VOB {1}.", BranchName, vob);
                    ccVob.CreateBranchType(BranchName, "Created by ClearCaseDetailedHistoryAdapter", CCTypeConstraint.ccConstraint_PerBranch, false, false);
                }
            }
        }

        /// <summary>
        /// Undo all checkouts under the specified path and then the path itself.
        /// </summary>
        /// <param name="path"></param>
        public void UndoCheckoutRecursive(string path)
        {
            List<string> checkoutFileList;
            string lsCheckoutCmdOutput = null;
            try
            {
                // Query checkout on the path itself.
                string lsCheckoutCmd = string.Format(ClearToolCommand.lscheckoutNonRecursive, ClearCasePath.MakeRelative(path));
                lsCheckoutCmdOutput = ExecuteClearToolCommand(lsCheckoutCmd);
                checkoutFileList = CCTextParser.ParseLSOutput(lsCheckoutCmdOutput);
                UndoCheckout(checkoutFileList);

                // Query all checkouts recursively.
                lsCheckoutCmd = string.Format(ClearToolCommand.lscheckoutRecursive, ClearCasePath.MakeRelative(path));
                lsCheckoutCmdOutput = ExecuteClearToolCommand(lsCheckoutCmd);
                checkoutFileList = CCTextParser.ParseLSOutput(lsCheckoutCmdOutput);
                UndoCheckout(checkoutFileList);
            }
            catch (COMException ce)
            {
                COMExceptionResult rslt = CCTextParser.ProcessComException(ce);
                if ((rslt == COMExceptionResult.NotAccessible)
                    || (rslt == COMExceptionResult.UnableToDetermineDynamicView)
                    || (rslt == COMExceptionResult.PathNotFound))
                {
                    return;
                }
                else
                {
                    throw;
                }
            }

        }

        public List<string> QueryViewPrivateFiles(string viewName)
        {
            List<string> viewPrivateFileList = new List<string>();
            try
            {
                foreach(string vob in VobList)
                {
                    string lsPrivateCmd = string.Format("lsprivate -tag \"{0}\" -invob \"{1}\" -other ", viewName, vob);
                    viewPrivateFileList.AddRange(
                        CCTextParser.ParseLSOutput(ExecuteClearToolCommand(lsPrivateCmd)));
                }
            }
            catch (COMException ce)
            {
                Trace.TraceWarning(string.Format("Failed to list private files for view \"{0}\". Error message {1}", viewName, ce.Message));
                throw;
            }

            return viewPrivateFileList;
        }

        public void UndoCheckout(List<string> checkoutFileList)
        {
            string uncheckoutCmd;
            string uncheckoutCmdOutput;
            if ((checkoutFileList == null) || (checkoutFileList.Count == 0))
            {
                return;
            }
            foreach (string checkoutFile in checkoutFileList)
            {
                uncheckoutCmd = string.Format("uncheckout -rm \"{0}\"", checkoutFile);
                try
                {
                    uncheckoutCmdOutput = ExecuteClearToolCommand(uncheckoutCmd);
                }
                catch (COMException ce)
                {
                    Trace.TraceWarning(string.Format("Failed to uncheckout element \"{0}\". Error message {1}", checkoutFile, ce.Message));
                }
            }
        }


        /// <summary>
        /// Create a configspec based on configuration file. 
        /// </summary>
        private void createConfigSpec()
        {
            StringBuilder configSpecRule = new StringBuilder(CCResources.ConfigSpecCheckOutRule);
            if (string.IsNullOrEmpty(BranchVersionString) ||
                string.Equals(BranchVersionString, "main", StringComparison.OrdinalIgnoreCase))
            {
                configSpecRule.AppendFormat(CCResources.ConfigSpecSelectMain);
            }
            else
            {
                configSpecRule.AppendFormat(string.Format(CCResources.ConfigSpecSelectBranch, BranchVersionString));
                configSpecRule.AppendFormat(string.Format(CCResources.ConfigSpecCreateBranch, BranchName));
            }

            foreach (string vob in VobList)
            {
                configSpecRule.AppendFormat(CCResources.ConfigSpecLoadRule, vob);
            }

            SetDefaultConfigSpec(configSpecRule.ToString());
        }

        /// <summary>
        /// Set the config spec to add the "time 000:00:00" used by snapshot start time.
        /// </summary>
        /// <param name="snapshotStartTime"></param>
        public void SetConfigSpecForSnapshotStartPoint(DateTime snapshotStartTime)
        {
            if (snapshotStartTime == DateTime.MinValue)
            {
                return;
            }

            string newConfigSpecPath = Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), "NewConfigSpec.txt");

            //Fire the cleartool command to cat config spec
            string cmdOutput = ExecuteClearToolCommand(ClearToolCommand.catcs);

            try
            {
                using (StreamWriter sw = new StreamWriter(newConfigSpecPath))
                {
                    // Add some text to the file.
                    sw.WriteLine(string.Format("time {0}", snapshotStartTime.ToString("dd-MMM-yyyy.HH:mm:ss")));
                    using (StringReader sr = new StringReader(cmdOutput))
                    {
                        while (sr.Peek() >= 0)
                        {
                            sw.WriteLine(sr.ReadLine());
                        }
                    }
                }
            }
            catch (IOException)
            {
                throw;
            }

            string setConfigSpecCmd = string.Format(ClearToolCommand.setcs, newConfigSpecPath);
            ExecuteClearToolCommand(setConfigSpecCmd);
        }

        /// <summary>
        /// Reset the config spec to remove the "time 000:00:00" set by the snapshotstart time change.
        /// </summary>
        public void ResetConfigSpec()
        {
            string newConfigSpecPath = Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), "NewConfigSpec.txt");

            //Fire the cleartool command to cat config spec
            string cmdOutput = ExecuteClearToolCommand(ClearToolCommand.catcs);

            try
            {
                using (StreamWriter sw = new StreamWriter(newConfigSpecPath))
                {
                    using (StringReader sr = new StringReader(cmdOutput))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string line = sr.ReadLine();
                            if (line.StartsWith("time"))
                            {
                                continue;
                            }
                            sw.WriteLine(line);
                        }
                    }
                }
            }
            catch (IOException)
            {
                throw;
            }

            string setConfigSpecCmd = string.Format(ClearToolCommand.setcs, newConfigSpecPath);
            ExecuteClearToolCommand(setConfigSpecCmd);
        }

        /// <summary>
        /// Initialize the clearcase server
        /// 1. Mount the Vob
        /// 2. Create the default view.
        /// 3. Create config spec for the view. 
        /// </summary>
        public void Initialize()
        {
            // Create the default view
            if (UsePrecreatedView)
            {
                if (UseDynamicView)
                {
                    StartView(ViewName);
                }
                else
                {
                    Update(ViewRootLocalPath);
                }
            }
            else if (!IsViewExists(ViewName))
            {
                CreateNewView(ViewName, StorageLocation);
            }

            // Set current directory to the view's root path
            SetDefaultViewPath();

            if (!UsePrecreatedView)
            {
                createConfigSpec();
            }
        }

        /// <summary>
        /// Clean up the COM object hold by this ClearCaseServer object.
        /// </summary>
        private void cleanUp()
        {
            ApplicationClass = null;
            m_clearCaseTool = null;
        }

        /// <summary>
        /// Returns true if the specified view exists on the clearcase server
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        internal bool IsViewExists(string viewName)
        {
            CCViews viewCollection = ApplicationClass.get_Views(false, string.Empty);

            foreach (CCView view in viewCollection)
            {
                if (view.TagName == viewName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Mount the specified Vob.
        /// </summary>
        /// <param name="vobName"></param>
        internal void MountVob(string vobName)
        {
            string mountVObCmd =
                string.Format(ClearToolCommand.mount, vobName);
            ExecuteClearToolCommand(mountVObCmd);
        }

        /// <summary>
        /// Set the current working director to the view's default path
        /// </summary>
        internal void SetDefaultViewPath()
        {
            string changeDirCmd = string.Format("cd {0}", ViewRootLocalPath);
            try
            {
                ExecuteClearToolCommand(changeDirCmd);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sets defaule config spec for view
        /// </summary>
        /// <param name="configSpecRule">Spec rule</param>
        private void SetDefaultConfigSpec(string configSpecRule)
        {
            //Check for null input
            if (string.IsNullOrEmpty(configSpecRule))
            {
                throw new ArgumentNullException("configSpecRule");
            }

            string configSpecPath = GetDefaultConfigSpecPath(configSpecRule);

            //Form the command string for setting config spec
            string setConfigSpecCmd = string.Format(ClearToolCommand.setcs, configSpecPath);

            //Fire the cleartool command to set config spec
            ExecuteClearToolCommand(setConfigSpecCmd);
        }

        /// <summary>
        /// Add a new load rule of the specified pname into the current view's config spec.
        /// </summary>
        /// <param name="vobName"></param>
        public void AddLoadRuleToConfigSpec(string pName)
        {
            string newConfigSpec = ClearCaseConfigSpec.AddLoadRule(ClearCaseView.ConfigSpec, pName);
            ClearCaseView.ConfigSpec = newConfigSpec;
        }

        /// <summary>
        /// Update the loaded elements
        /// </summary>
        /// <param name="pName"></param>
        public void Update(string pName)
        {
            if (string.IsNullOrEmpty(pName))
            {
                return;
            }
            Debug.Assert(!UseDynamicView, "Clearcase dynamic view cannot be updated");
            string updateCommandString = string.Format(ClearToolCommand.update, pName);
            ExecuteClearToolCommand(updateCommandString);            
        }

        /// <summary>
        /// Update a list of elements
        /// </summary>
        /// <param name="pNameList"></param>
        public void Update(List<string> pNameList)
        {
            if ((pNameList == null) || (pNameList.Count == 0))
            {
                return;
            }
            Debug.Assert(!UseDynamicView, "Clearcase dynamic view cannot be updated");
            StringBuilder pNameStringBuilder = new StringBuilder();
            foreach (string pName in pNameList)
            {
                pNameStringBuilder = pNameStringBuilder.Append(string.Format("'{0}' ", pName));
            }

            ExecuteClearToolCommand(string.Format(ClearToolCommand.update, pNameStringBuilder.ToString()));
        }

        /// <summary>
        /// Create view with specified name and default config spec
        /// </summary>
        /// <param name="viewName">Name of view</param>
        /// <param name="storageLocation">storage location name</param>
        public void CreateNewView(string viewName, string storageLocation)
        {
            //Form the mkview command
            string mkViewCommandString = string.Format(ClearToolCommand.mkviewSnapshot, viewName, ViewRootPath);
            ExecuteClearToolCommand(mkViewCommandString);
        }

        internal void RemoveView(string viewName, string storageLocation)
        {
            if (!UsePrecreatedView)
            {
                //Delete snapshot view
                string fullStoragePath = ClearCasePath.Combine(storageLocation, viewName);

                //Form the rmview command
                string rmViewCommandString = string.Format(ClearToolCommand.rmview, fullStoragePath);
                ExecuteClearToolCommand(rmViewCommandString);
            }
        }

        /// <summary>
        /// Given a view local path, return the absolute vob path. 
        /// </summary>
        /// <param name="viewLocalPath"></param>
        /// <returns></returns>
        public string GetServerPathFromViewLocalPath(string viewLocalPath)
        {
            if (string.IsNullOrEmpty(viewLocalPath))
            {
                return null;
            }

            string viewRootPath = ViewRootPath;
            if (viewRootPath[viewRootPath.Length-1] == ClearCasePath.Separator)
            {
                viewRootPath = viewRootPath.Substring(0, viewRootPath.Length - 1);
            }
            if (ClearCasePath.IsSubItem(viewLocalPath, viewRootPath, false))
            {
                return viewLocalPath.Substring(viewRootPath.Length);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Given a server path, return the view local path.
        /// </summary>
        /// <param name="serverPath"></param>
        /// <returns></returns>
        public string GetViewLocalPathFromServerPath(string serverPath)
        {
            if (serverPath == null)
            {
                return null;
            }

            return ClearCasePath.Combine(ViewRootPath, serverPath);
        }

        public string GetCheckinPathFromServerPath(string serverPath)
        {
            return GetVobAbsolutePathFromRelativePath(serverPath);
        }

        public bool IsVob(string path)
        {
            if (VobList.Contains(path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private string GetViewPath(string extendedPath, string versionPath, string datasourcePath)
        {
            //Check for null input parameters
            if (string.IsNullOrEmpty(extendedPath))
                throw new ArgumentNullException("extendedPath");

            if (string.IsNullOrEmpty(datasourcePath))
                throw new ArgumentNullException("datasourcePath");

            //Form the element path starting from vob name
            string vobPrefixedPath = string.Empty;
            string viewExtendedPath = string.Empty;

            extendedPath = extendedPath.TrimStart('\\');

            //if element path is not root folder
            if (ClearCasePath.Equals(extendedPath, ClearCasePath.VobRoot))
            {
                // s_elementPath = s_elementPath.TrimStart('.');

                string startStr = datasourcePath + '\\' + ViewName + datasourcePath;

                // In some cases extended path can contain datasource path 
                if (extendedPath.StartsWith(startStr))
                {
                    if (string.IsNullOrEmpty(versionPath))
                    {
                        viewExtendedPath = extendedPath;
                    }
                    else
                    {
                        viewExtendedPath = ClearCasePath.CombinePathWithVersion(extendedPath, versionPath);
                    }

                    return viewExtendedPath;
                }
                else
                {
                    if (string.IsNullOrEmpty(versionPath))
                    {
                        vobPrefixedPath = datasourcePath + ClearCasePath.Separator + extendedPath;
                    }
                    else
                    {
                        vobPrefixedPath = ClearCasePath.CombinePathWithVersion(
                            datasourcePath + ClearCasePath.Separator + extendedPath, 
                            versionPath);
                    }
                }
            }
            else //if root folder
            {
                //if root folder is entire VOB
                if (datasourcePath == ClearCasePath.GetVobName(datasourcePath))
                {
                    if (string.IsNullOrEmpty(versionPath))
                    {
                        vobPrefixedPath = datasourcePath + "\\.";
                    }
                    else
                    {
                        vobPrefixedPath = ClearCasePath.CombinePathWithVersion(
                            datasourcePath + "\\." ,
                            versionPath);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(versionPath))
                    {
                        vobPrefixedPath = datasourcePath;
                    }
                    else
                    {
                        vobPrefixedPath = ClearCasePath.CombinePathWithVersion(datasourcePath, versionPath);
                    }
                }
            }
            viewExtendedPath = ViewRootPath + vobPrefixedPath;
            return viewExtendedPath;
        }

        internal string GetRootElementPath(string sourcePath)
        {
            if (UsePrecreatedView)
            {
                return ClearCasePath.Combine(ViewRootPath, sourcePath);
            }
            else
            {
                CCElement rootElement = queryCCElement(ClearCasePath.GetViewExtendedPath(ClearCasePath.VobRoot.ToString(), sourcePath, ViewName, StorageLocation));
                if (rootElement.Path.EndsWith(ClearCasePath.VobRoot.ToString()))
                {
                    return rootElement.Path.Substring(0, rootElement.Path.Length - ClearCasePath.VobRoot.ToString().Length);
                }
                else
                {
                    return rootElement.Path;
                }
            }
        }

        internal void analyzeHistoryRecord(CCHistoryRecord historyRecord)
        {
            throw new NotImplementedException("analyzeHistoryRecord");
        }

        public List<CCHistoryRecord> GetHistoryRecords(
            ReadOnlyCollection<MappingEntry> filters, 
            DateTime since,
            bool writeHistoryRecordsFound)
        {
            List<CCHistoryRecord> historyRecordList = new List<CCHistoryRecord>();

            string extendedPath;
            string historyCmd;
            string cmdOutput = null;
            foreach (MappingEntry mapping in filters)
            {
                //Creates extended path for source folder.
                //Query from the parent of the mapped path unless the mapped path itself is a vob root.
                if (UsePrecreatedView)
                {
                    if (!ClearCasePath.IsVobRoot(mapping.Path))
                    {
                        extendedPath = ClearCasePath.GetFolderName(mapping.Path);
                    }
                    else
                    {
                        extendedPath = mapping.Path;
                    }
                    historyCmd = string.Format(
                    "lshistory -minor -since {0} -eventid -recurse -fmt \"{1}\" -pname {2}",
                    since.ToString(CCResources.ClearCaseDateTimeFormat),
                    ClearCaseCommandSpec.HistoryParserString,
                    extendedPath);
                }
                else
                {
                    if (!ClearCasePath.IsVobRoot(mapping.Path))
                    {
                        extendedPath = ClearCasePath.GetViewExtendedPath(".", 
                            ClearCasePath.GetFolderName(mapping.Path), ViewName, StorageLocation);
                    }
                    else
                    {
                        extendedPath = ClearCasePath.GetViewExtendedPath(".", mapping.Path, ViewName, StorageLocation);
                    }
                    historyCmd = string.Format(
                    "lshistory -minor -since {0} -branch {1} -eventid -fmt \"{2}\" -pname {3}",
                    since.ToString(CCResources.ClearCaseDateTimeFormat),
                    BranchName,
                    ClearCaseCommandSpec.HistoryParserString,
                    extendedPath);
                }
                try
                {
                    cmdOutput = ExecuteClearToolCommand(historyCmd);
                }
                catch (COMException ce)
                {
                    COMExceptionResult comResult = CCTextParser.ProcessComException(ce);
                    if (comResult == COMExceptionResult.NotAccessible
                        || comResult == COMExceptionResult.BranchTypeNotFound
                        || comResult == COMExceptionResult.PathNotFound)
                    {
                        continue;
                    }
                    throw;
                }

                if (string.IsNullOrEmpty(cmdOutput))
                {
                    TraceManager.TraceInformation(string.Format("No history event was found under path {0}", extendedPath));
                }
                else
                {
                    string[] historyRows = ClearCaseCommandSpec.ParseHistoryTable(cmdOutput);
                    CCHistoryRecord historyRecord;
                    foreach (string historyRow in historyRows)
                    {
                        historyRecord = new CCHistoryRecord(historyRow);
                        if (Utils.IsOurChange(historyRecord))
                        {
                            continue;
                        }
                        if (writeHistoryRecordsFound)
                        {
                            WriteHistoryRecord(historyRecord);
                        }
                        historyRecordList.Add(historyRecord);
                    }
                }
            }

            return historyRecordList;
        }

        internal void WriteHistoryRecord(CCHistoryRecord historyRecord)
        {
            // Todo - verbose information to be deleted.
            TraceManager.TraceInformation("||{0} | {1} | {2} | {3} | {4} | {5} | {6} ", historyRecord.EventId, historyRecord.OperationType, 
                historyRecord.OperationDescription, historyRecord.Comment, historyRecord.VersionTime, historyRecord.UserComment, historyRecord.VersionExtendedPath);
        }

        #region algorithms
        #endregion

        internal CCElement queryCCElement(string elementPath)
        {
            CCElement ccElement= null;
            try
            {
                ccElement = ApplicationClass.get_Element(elementPath);
            }
            catch (Exception)
            {
                throw ;
            }
            return ccElement;
        }

        /* Todo
        /// <summary>
        /// Traverse the version tree of the given element. 
        /// </summary>
        /// <param name="element"></param>
        private void TraverseVersionTree(CCElement element)
        {
            bool mainBranchTraversed = m_startFromMiddle;

            // Re-intialize the buffer which contains all the parents
            // of the currentElement and all their branches.
            m_parentsAndTheirBranches = new Dictionary<string, Collection<CCBranchType>>();

            while (m_branchCollection.Count > 0 || !mainBranchTraversed)
            {
                CCBranch currentBranch = null;
                CCBranchType currentBranchType = null;

                // Traverse the main branch if the main branch has not been traversed.
                if (!mainBranchTraversed)
                {
                    currentBranchType = m_branchTypeCollection[ClearCasePath.MainBranchName];
                }
                else
                {
                    currentBranch = m_branchCollection[0];
                    m_branchCollection.RemoveAt(0);
                    currentBranchType = currentBranch.Type;                    
                }

                if (currentBranch != null)
                {
                    TraverseBranch(element, currentBranch.BranchPointVersion, currentBranch.Type);
                }
                else
                {
                    TraverseBranch(element, null, currentBranchType);
                }
            }

            m_startFromMiddle = false;
            m_traversedBranchTypes.Clear();
        }

        Todo, 
         * 
         * private void TraverseBranch(CCElement element, CCVersion branchPointVersion, CCBranchType branchType)
        {
            // Branch type is not listed.
            if (!m_branchTypeCollection.ContainsKey(branchType.Name))
            {
                return;
            }

            // Add the branchType which is being traversed to traversed list.
            m_traversedBranchTypes.Add(branchType);

            CCVersion currentVersion = null;

            string branchSelector = branchType.Name;

            if (m_startingVersion == null)
            {
                // Get the first version of the given element on the given branch.
                currentVersion = element.get_Version(ClearCasePath.CombineVersionExtendedPath(element.ExtendedPath, branchType.Name, "0"));
            }
            else
            {
                // Before starting with the next version make sure that 
                // the processing for the last version which was added before 
                // the crash is completed.
                UpdateBranchCollection(m_startingVersion, element);
                ProcessFolderVersion(m_startingVersion, element);

                // Get he next version in case the starting version is
                // not null as in case of crash recovery we need to get 
                // the next version which to be traversed.
                currentVersion = GetNextVersion(m_startingVersion);
            }

            while (currentVersion != null)
            {
                // Add file/folder versions to the database
                bool versionAdded = AddVersion(
                    MigrationManager.CurrentMapping,
                    currentVersion,
                    element);

                // Update the collections of branches which need to 
                // be traversed
                UpdateBranchCollection(currentVersion, element);


                if (versionAdded)
                {
                    // Get the details of the folder operations performed 
                    // on this version 
                    ProcessFolderVersion(
                        currentVersion,
                        element);

                    // Generate task for the extractor component
                    GenerateTask(currentVersion);
                }

                currentVersion = element.GetNextVersion(currentVersion);
            }
        }

        /// <summary>
        /// NOTE - Not implemented as of now.
        /// Gets the next version selected by the view for the
        /// specified version
        /// IMP - Consider case in which multiple branches sprout
        /// from a version.
        /// </summary>
        /// <returns>ClearCaseFileVersion object</returns>
        internal Version GetNextVersion(Version inputVersion)
        {
            if (inputVersion == null)
                throw new ArgumentNullException("inputVersion");

            ClearCaseFileVersion converterFileVersion = null;


            ClearCaseFileVersion ccInputVersion = inputVersion as ClearCaseFileVersion;


            string branchAbsolutePath = GetBranchAbsolutePath(inputVersion);

            CCVersion previousCCVersion = null;
            CCVersion nextCCVersion = null;

            try
            {
                if (ccInputVersion != null)
                {
                    //Get previous CCVersion for the specified input
                    previousCCVersion = ccInputVersion.CalVersion;
                }
                else
                {
                    //Get previous CCVersion for the specified input
                    previousCCVersion = ClearCaseHelper.GetCCVersion( this.ccFileElement, branchAbsolutePath, (int)inputVersion.VersionNo);
                }
            }
            catch (Exception dataExp)
            {
                //Todo
                return null;
            }

            // If input verison is not the latest version then
            // get the next version
            if (!previousCCVersion.IsLatest)
            {
                int nextVersionNumber = (int)inputVersion.VersionNo;
                nextVersionNumber++;

                while (nextVersionNumber <= previousCCVersion.Branch.LatestVersion.VersionNumber)
                {
                    try
                    {
                        nextCCVersion = GetCCVersion(this.ccFileElement, branchAbsolutePath, nextVersionNumber);
                        break;
                    }
                    catch (Exception)
                    {
                        // Todo DataNotAvailableException
                        //MigrationTrace.WriteTrace(
                        //    MigrationTraceLevel.Low,
                        //    string.Format(
                        //        CultureInfo.InvariantCulture,
                        //        "Version {0} does not exist for file {1}. " +
                        //        "Hence incrementating the version count number.",
                        //        new object[] { branchAbsolutePath + nextNo, this.Path }),
                        //    "ClearCaseFile.GetNextVersion");

                        nextVersionNumber++;
                    }
                }

                // If the nextCCVersion is not null then get the ClearCase
                // file version
                if (nextCCVersion != null)
                {
                    CCBranchType converterBranchType = m_branchTypeCollection[nextCCVersion.Branch.Type.Name];

                    try
                    {
                        converterFileVersion =
                            new ClearCaseFileVersion(
                                (uint)nextCCVersion.VersionNumber,
                                nextCCVersion.CreationRecord.Date,
                                nextCCVersion.Identifier,
                                converterBranchType,
                                this.Id,
                                this.Path,
                                nextCCVersion.CreationRecord.UserLoginName,
                                nextCCVersion.Comment,
                                nextCCVersion);
                    }
                    catch (Exception exp)
                    {
                        //Todo

                    }
                }
            }

            return converterFileVersion;
        }


        private void ProcessFolderVersion(Version version, CCElement element)
        {
            FolderVersion folderVersion = version as FolderVersion;
            if (folderVersion != null)
            {
                // Get all the folder operation and add all the 
                // newly added files which are to be traversed in 
                // the files to be traversed collection
                Collection<FolderOperation> operations = folderVersion.FolderOperations;

                foreach (FolderOperation operation in operations)
                {
                    RenameOperation renOperation =
                            operation as RenameOperation;

                    if (operation is AddOperation)
                    {
                        // Process add operation 
                        ProcessAddOperation(operation, folderVersion, element);
                    }
                    else if (renOperation != null)
                    {
                        // Process rename operation
                        ProcessRenameOperation(renOperation, element);
                    }
                    else if (operation is DeleteOperation)
                    {
                        // Process the delete operation
                        ProcessDeleteOperation(operation);
                    }
                    else if (operation is NullOperation)
                    {
                        // Process the null operation
                        this.ProcessNullOperation(operation);
                    }
                }
            }
        }


        private void UpdateBranchCollection(Version currentVersion, CCElement element)
        {
            // Add all the branches which are on the current version 
            // in the branches to be traversed collection
            // NOTE: It is assumed here that no branch will come out 
            // of the same version
            foreach (Branch currentBranch in currentVersion.Branches)
            {
                // If the traversedBranchTypes already contains the 
                // branch then ignore this branch and log an 
                // error
                if (m_traversedBranchTypes.Contains(currentBranch.Type))
                {
                    string message =  string.Format(
                            CultureInfo.InvariantCulture,
                            CCResources.ErrorMultipleBranchInstance,
                            new object[] { 
                                    element.ToString(), 
                                    currentBranch.Type, 
                                    currentVersion.ToString() });
                    throw new NotImplementedException();
                    continue;
                }

                m_branchCollection.Add(currentBranch);

                // Todo:
                // Add the branch in the branch version map this needs to be 
                // done because suppose if extractor is adding the branch info
                // in branchversion map then if the tool is closed when the
                // traverser marks a branch as UnProcessed and the extractor has
                // not yet added the branch info in branch version map. In such 
                // a case the GetPendingBranches() fn fails as it does not get 
                // a branchpoint version from the DB.
                // AddBranch(curBranch, currentVersion, databaseServer);

                // Todo, mark the status of the branch as UnProcessed 
                // as this branch needs to be traversed upon
            }
        }


        // Comment out for now. 

        /// <summary>
        /// Gets collection of CCBranchType on specfied Vob path
        /// </summary>
        /// <returns>Collection of CCBranchType</returns>
        internal CCBranchTypes GetBranchTypes()
        {
            try
            {
                //Get the CCBranchTypes on specified vob
                CCBranchTypes branchTypes = m_vob.get_BranchTypes(false, false);                
                return branchTypes;
            }
            catch (Exception ex)
            {
                throw new NotImplementedException();
            }
        }


        /// <summary>
        /// Gets collection of ClearCaseBranchTypes on specified vob
        /// </summary>
        /// <returns>Dictionary of ClearCaseBranchTypes</returns>
        private void getBranchTypeCollection()
        {
            if (m_branchTypeCollection == null || m_branchTypeCollection.Count <= 0)
            {
                CCBranchTypes ccBranchTypes = GetBranchTypes();

                m_branchTypeCollection = new Dictionary<string, CCBranchType>();

                foreach (CCBranchType ccBranchType in ccBranchTypes)
                {
                    if (m_branchTypeCollection.ContainsKey(ccBranchType.Name))
                    {
                        continue;
                    }

                    CCBranchType branchType;
                    if (m_branches.Contains(ccBranchType.Name) || ccBranchType.Name == ClearCasePath.MainBranchName)
                    {
                        branchType = new CCBranchType(ccBranchType.Name, true);
                    }
                    else
                    {
                        branchType = new CCBranchType(ccBranchType.Name, false);
                    }
                }
            }
        }*/

        /// <summary>
        /// Starts the specified view
        /// </summary>
        /// <param name="viewName">Name of the view.</param>
        private void StartView(string viewName)
        {
            if (string.IsNullOrEmpty(viewName))
                throw new ArgumentNullException("viewName");

            string cmd = string.Format(ClearToolCommand.startview, viewName);
            ExecuteClearToolCommand(cmd);
        }

        /// <summary>
        /// Find symbolic links of a vob and store them in the supplied hashset. 
        /// </summary>
        /// <param name="vob">Vob to be searched for.</param>
        /// <param name="symbolicLinks">List for symbolic links found.</param>
        public void FindSymbolicLink(string vob, HashSet<string> symbolicLinks)
        {
            if (symbolicLinks == null)
            {
                throw new ArgumentNullException("symbolicLinks");
            }
            if (string.IsNullOrEmpty(vob))
            {
                throw new ArgumentNullException("vob");
            }

            string findOutput = ExecuteClearToolCommand(string.Format(ClearToolCommand.findSymbolicLink, ClearCasePath.MakeRelative(vob)));

            if (string.IsNullOrEmpty(findOutput))
            {
                return;
            }

            string outputLine;
            using (StringReader outputReader = new StringReader(findOutput))
            {
                while (true)
                {
                    outputLine = outputReader.ReadLine();
                    if (outputLine == null)
                    {
                        break;
                    }
                    else
                    {
                        // Todo verify it is a valid path
                        if (!symbolicLinks.Contains(outputLine))
                        {
                            symbolicLinks.Add(outputLine);
                        }
                    }
                }
            }
        }

        private static string GetDefaultConfigSpecPath(string specRule)
        {
            string configSpecPath = string.Empty;

            // Create a temp file to hold the default config spec
            try
            {
                configSpecPath = Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), "ConfigSpec.txt");
                using (StreamWriter sw = new StreamWriter(configSpecPath))
                {
                    // Add some text to the file.
                    sw.Write(specRule);
                }
            }
            catch (IOException)
            {
                throw;
            }

            return configSpecPath;
        }


        /// <summary>
        /// Execute a clearcase command.
        /// </summary>
        /// <param name="commandString"></param>
        /// <returns></returns>
        public string ExecuteClearToolCommand(string commandString)
        {
            string commandOutput = string.Empty;

            try
            {
                commandOutput = m_clearCaseTool.CmdExec(commandString);
                return commandOutput;
            }
            catch (Exception)
            {
                throw ;
            }
        }

        /// <summary>
        /// Exceutes a command by forking a new process
        /// Some command cannot be executed from ClearToolClass object.
        /// </summary>
        /// <param name="cmdString">command string to be executed</param>
        /// <param name="isError">bool indicating if the command execution 
        /// resulted in an error</param>
        /// <returns>output string</returns>
        internal static string ExecuteCommand(string cmdString, out bool isError)
        {
            //Check for null input
            if (string.IsNullOrEmpty(cmdString))
                throw new ArgumentNullException("cmdString");

            // /C carries out command from within cmd shell and then terminates 
            cmdString = string.Format("/c {0}", cmdString);
            string output = string.Empty;
            isError = false;
            string errorString;

            try
            {
                using (Process process = new Process())
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd.exe", cmdString);
                    processStartInfo.UseShellExecute = false;
                    processStartInfo.RedirectStandardError = true;
                    processStartInfo.CreateNoWindow = true;
                    process.StartInfo = processStartInfo;
                    process.Start();

                    StreamReader errorStreamReader = process.StandardError;

                    // Read the standard output of the spawned process.
                    errorString = errorStreamReader.ReadToEnd();
                    errorStreamReader.Close();

                }
                if (errorString.Length > 0)
                {
                    isError = true;
                    output = errorString;
                }

                return output;
            }
            catch (Exception)
            {
                throw;
            }
        }
        internal static Dictionary<string,ClearfsimportResult> ExecuteClearfsimportCommand(string cmdString)
        {
            if (string.IsNullOrEmpty(cmdString))
            {
                throw new ArgumentNullException("cmdString");
            }
            // /C carries out command from within cmd shell and then terminates 
            cmdString = string.Format("/c {0}", cmdString);

            s_clearfsimportOutput.Clear();
            s_clearfsimportErrorOutput.Clear();
            try
            {
                using (Process process = new Process())
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd.exe", cmdString);
                    processStartInfo.UseShellExecute = false;
                    processStartInfo.RedirectStandardError = true;
                    processStartInfo.RedirectStandardOutput = true;
                    processStartInfo.CreateNoWindow = true;
                    process.StartInfo = processStartInfo;
                    process.OutputDataReceived += new DataReceivedEventHandler(ClearfsimportDataOutputHandler);
                    process.ErrorDataReceived += new DataReceivedEventHandler(ClearfsimportDataErrorHandler);
                    process.Start();

                    // Read the standard output of the spawned process.
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    process.Close();
                }
                if (s_clearfsimportErrorOutput.Count > 0)
                {
                    // We just write a trace error here. 
                    // CC adapter uses s_clearfsimportOutput to determine whether an import is successful or not. 
                    foreach (string errorLine in s_clearfsimportErrorOutput)
                    {
                        TraceManager.TraceError("Clearfsimport error: {0}", errorLine);
                    }
                }

                return s_clearfsimportOutput;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Generate a label for CC adapter check ins.
        /// </summary>
        /// <param name="changeGroupId"></param>
        /// <returns></returns>
        internal static string GenerateCheckinLabelType(long changeGroupId)
        {
            return string.Format(CCResources.CheckinLabelFormat, DateTime.Now);
        }

        private static void ClearfsimportDataOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            TraceManager.TraceVerbose(outLine.Data);
            bool addEntry = false;
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                if (outLine.Data.StartsWith("Creating directory \"", StringComparison.Ordinal))
                {
                    s_elementPath = outLine.Data.Substring(20);
                    Debug.Assert(s_elementPath.Length > 2, "Error element path");
                    s_elementPath = s_elementPath.Substring(0, s_elementPath.Length -2);
                    s_previousState = ClearfsimportResult.CreateDirectory;
                    addEntry = true;
                }
                else if (outLine.Data.StartsWith("Validating directory \"", StringComparison.Ordinal))
                {
                    s_elementPath = outLine.Data.Substring(22);
                    Debug.Assert(s_elementPath.Length > 2, "Error element path");
                    s_elementPath = s_elementPath.Substring(0, s_elementPath.Length - 2);
                    s_previousState = ClearfsimportResult.ValidatingDirectory;
                    addEntry = true;
                }
                else if (outLine.Data.StartsWith("Creating element \"", StringComparison.Ordinal))
                {
                    s_elementPath = outLine.Data.Substring(18);
                    Debug.Assert(s_elementPath.Length > 2, "Error element path");
                    s_elementPath = s_elementPath.Substring(0, s_elementPath.Length - 2);
                    s_previousState = ClearfsimportResult.CreateElement;
                    addEntry = true;
                }
                else if (outLine.Data.StartsWith("Validating element \"", StringComparison.Ordinal))
                {
                    s_elementPath = outLine.Data.Substring(20);
                    Debug.Assert(s_elementPath.Length > 2, "Error element path");
                    s_elementPath = s_elementPath.Substring(0, s_elementPath.Length - 2);
                    s_previousState = ClearfsimportResult.ValidatingElement;
                }
                else if ((outLine.Data.StartsWith("Created branch \"", StringComparison.Ordinal))
                    && (s_previousState == ClearfsimportResult.ValidatingElement))
                {
                    s_previousState = ClearfsimportResult.UpdateElement;
                    addEntry = true;
                }
                else if ((outLine.Data.StartsWith("    update version \"", StringComparison.Ordinal))
                    && (s_previousState == ClearfsimportResult.ValidatingElement))
                {
                    s_previousState = ClearfsimportResult.UpdateElement;
                    addEntry = true;
                }
                else if ((outLine.Data.StartsWith("    Skipping element \"", StringComparison.Ordinal))
                    && (s_previousState == ClearfsimportResult.ValidatingElement))
                {
                    s_previousState = ClearfsimportResult.UpdateElement;
                    addEntry = true;
                }
                else
                {
                    s_elementPath = string.Empty;
                }

                if (addEntry)
                {
                    if (s_clearfsimportOutput.ContainsKey(s_elementPath))
                    {
                        TraceManager.TraceInformation(string.Format("Item {0} already added to the clearfsimport output", s_elementPath));
                    }
                    else
                    {
                        s_clearfsimportOutput.Add(s_elementPath, s_previousState);
                    }
                }
            }
        }

        private static void ClearfsimportDataErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                string errorLine = CCTextParser.ParseClearfsimportErrorOutput(outLine.Data);
                if (!string.IsNullOrEmpty(errorLine))
                {
                    s_clearfsimportErrorOutput.Add(errorLine);
                }
            }
        }
    }
}
