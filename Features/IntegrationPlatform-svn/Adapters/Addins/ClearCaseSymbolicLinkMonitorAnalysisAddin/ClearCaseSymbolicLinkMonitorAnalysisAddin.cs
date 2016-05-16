// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Text;

using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

using ClearCase;

namespace ClearCaseSymbolicLinkMonitorAnalysisAddin
{
    /// <summary>
    /// An AnalysisAddin that can be used with with the ClearCaseSelectedHistoryAdapter to create a file based on the
    /// list of symbolic links found in the VOBs referenced when migrating/syncing from ClearCase to TFS.
    /// The output file will contain one line with the path of a TFS item that corresponds to a symbol link in 
    /// ClearCase.
    /// The output file path can be configured using a CustomSetting with the key name "SymbolLinkOutputFilePath".
    /// If that key is not present, the output filename defaults to "ClearCaseSymbolLinksMigrated.txt" in the
    /// current working directory.
    /// </summary>
    public class ClearCaseSymbolicLinkMonitorAnalysisAddin : AnalysisAddin
    {

        /// <summary>
        /// The GUID string of the Reference Name of this Add-in
        /// </summary>
        public const string ReferenceNameString = "7AF45F88-7D99-48F2-9B15-673E619B5ED8";

        private const string c_symbolLinkOutputFilePathKey = "SymbolLinkOutputFilePath";
        private const string c_defaultSymbolLinkOutputFilePath = "ClearCaseSymbolLinksMigrated.txt";
        private const string c_findSymbolicLink = "find {0} -type l -print";

        private string m_symbolLinkOutputFilePath;

        private HashSet<string> m_vobNames = new HashSet<string>();

        private ConfigurationService m_configurationService;
        private IServerPathTranslationService m_serverPathTranslationService;
        private VCTranslationService m_vcTranslationService;
        private Guid m_sourceId;

        private ClearToolClass m_clearCaseTool;

        private ClearToolClass ClearCaseTool
        {
            get
            {
                if (m_clearCaseTool == null)
                {
                    m_clearCaseTool = new ClearToolClass();
                }
                return m_clearCaseTool;
            }
        }

        #region IAddin Members

        /// <summary>
        /// The Reference Name of this Add-in
        /// </summary>
        public override Guid ReferenceName
        {
            get { return new Guid(ReferenceNameString); }
        }

        public override string FriendlyName
        {
            get
            {
                return ClearCaseSymbolicLinkMonitorAnalysisAddinResources.AddinFriendlyName;
            }
        }

        public override ReadOnlyCollection<string> CustomSettingKeys
        {
            get
            {
                List<string> customSettingKeys = new List<string>();
                customSettingKeys.Add(c_symbolLinkOutputFilePathKey);
                return new ReadOnlyCollection<string>(customSettingKeys);
            }
        }

        public override ReadOnlyCollection<Guid> SupportedMigrationProviderNames
        {
            get
            {
                List<Guid> supportedMigrationProviders = new List<Guid>();

                // ClearCase Selected History Adapter for Tfs2010
                supportedMigrationProviders.Add(new Guid("B9379F30-2026-4d36-92A6-9654ABF91BFD"));

                // ClearCase Selected History Adapter for Tfs2008
                supportedMigrationProviders.Add(new Guid("F65A4623-3856-4507-B5E9-AD28811FD37E"));

                // ClearCase Detailed History Adapter
                supportedMigrationProviders.Add(new Guid("F2A6BA65-8ACB-4cd0-BE8F-B25887F94392"));

                return new ReadOnlyCollection<Guid>(supportedMigrationProviders);                
            }
        }

        #endregion

        #region AnalysisAddin implementation
        public override void PreAnalysis(AnalysisContext analysisContext)
        {
            m_configurationService = analysisContext.TookitServiceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;
            if (m_configurationService == null)
            {
                throw new ArgumentNullException("ConfigurationService");
            }
            m_serverPathTranslationService = (IServerPathTranslationService)((analysisContext.TookitServiceContainer.GetService(typeof(IServerPathTranslationService)) as IServerPathTranslationService));
            if (m_serverPathTranslationService == null)
            {
                throw new ArgumentNullException("IServerPathTranslationService");
            }
            m_vcTranslationService = (VCTranslationService)((analysisContext.TookitServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService));
            if (m_vcTranslationService == null)
            {
                throw new ArgumentNullException("ITranslationService");
            }
            m_sourceId = new Guid(m_configurationService.MigrationSource.InternalUniqueId);

            // Get the configured output file path here (or use default) and make sure we can open it for writing

            Dictionary<string, string> addinCustomSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            addinCustomSettings.Add(c_symbolLinkOutputFilePathKey, c_defaultSymbolLinkOutputFilePath);
            AddinCustomSettingsHelper.GetAddinCustomSettings(m_configurationService, ReferenceNameString, addinCustomSettings);
            m_symbolLinkOutputFilePath = addinCustomSettings[c_symbolLinkOutputFilePathKey];

            try
            {
                using (StreamWriter streamWriter = new StreamWriter(m_symbolLinkOutputFilePath, false))
                {
                }
            }
            catch (Exception e)
            {
                throw new MigrationException(string.Format(CultureInfo.InvariantCulture, ClearCaseSymbolicLinkMonitorAnalysisAddinResources.UnableToOpenOutputFile,
                    m_symbolLinkOutputFilePath, e.Message));
            }
        }


        public override void PostChangeGroupDeltaComputation(AnalysisContext analysisContext, ChangeGroup changeGroup)
        {
            // Find the set of all Vobs used in any change action
            foreach (IMigrationAction action in changeGroup.Actions)
            {
                string vobName = ClearCasePath.GetVobName(action.Path);
                if (!m_vobNames.Contains(vobName))
                {
                    m_vobNames.Add(vobName);
                }
            }
        }

        public override void PostAnalysis(AnalysisContext analysisContext)
        {
            HashSet<string> symbolicLinks = new HashSet<string>();
            // Find symbolic links in all VOB used
            foreach (string vob in m_vobNames)
            {
                FindSymbolicLinks(vob, symbolicLinks);
            }

            try
            {
                using (StreamWriter streamWriter = new StreamWriter(m_symbolLinkOutputFilePath, false))
                {
                    foreach (string symbolicLinkPath in symbolicLinks)
                    {
                        // Translate the symbolLinkPath to the path format used on the target side
                        try
                        {
                            string canonicalPath = m_serverPathTranslationService.TranslateToCanonicalPathCaseSensitive(symbolicLinkPath);
                            string translatedPath = m_vcTranslationService.GetMappedPath(canonicalPath, m_sourceId);
                            streamWriter.WriteLine(translatedPath);
                        }
                        catch (Exception ex)
                        {
                            TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture, "Unable to translate ClearCase Path '{0}': {1}",
                                symbolicLinkPath, ex.ToString()));
                            TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture, "Writing untranslated ClearCase Path to '{0}'",
                                m_symbolLinkOutputFilePath));
                            streamWriter.WriteLine(symbolicLinkPath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new MigrationException(string.Format(CultureInfo.InvariantCulture, ClearCaseSymbolicLinkMonitorAnalysisAddinResources.UnableToOpenOutputFile,
                    m_symbolLinkOutputFilePath, e.Message));
            }           
        }

        #endregion

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public override object GetService(Type serviceType)
        {
            if (serviceType == typeof(AnalysisAddin) ||
                serviceType == typeof(ClearCaseSymbolicLinkMonitorAnalysisAddin))
            {
                return this;
            }

            return null;
        }

        #endregion

        #region private methods
        /// <summary>
        /// Find symbolic links of a vob and store them in the supplied hashset. 
        /// </summary>
        /// <param name="vob">Vob to be searched for.</param>
        /// <param name="symbolicLinks">List for symbolic links found.</param>
        private void FindSymbolicLinks(string vob, HashSet<string> symbolicLinks)
        {
            if (symbolicLinks == null)
            {
                throw new ArgumentNullException("symbolicLinks");
            }
            if (string.IsNullOrEmpty(vob))
            {
                throw new ArgumentNullException("vob");
            }

            string findOutput = ExecuteClearToolCommand(string.Format(c_findSymbolicLink, ClearCasePath.MakeRelative(vob)));

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
                        // Prepend a \ to convert the output line to an absoluteVOB path
                        string symbolicLinkPath  = '\\' + outputLine;
                        // Todo verify it is a valid path
                        if (!symbolicLinks.Contains(symbolicLinkPath))
                        {
                            symbolicLinks.Add(symbolicLinkPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Execute a clearcase command.
        /// </summary>
        /// <param name="commandString"></param>
        /// <returns></returns>
        private string ExecuteClearToolCommand(string commandString)
        {
            string commandOutput = string.Empty;

            try
            {
                commandOutput = ClearCaseTool.CmdExec(commandString);
                return commandOutput;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
