// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;
using System.Xml;

namespace SemaphoreFileAnalysisAddin
{
    /// <summary>
    /// The SemaphoreFileAnalysisAddin is used with version control adapters like the File System adapter.  This add-in is intended
    /// to be used as part of an integration solution where a file on disk provides three key capabilities in the version control
    /// sync pipeline: 1) gating the flow of changes from disk (the file system) to TFS based on last modified/created time, 2) providing 
    /// changeset comment metadata, 3) providing label metadata.  Taken together, this add-in enables the FileSystem adapter to be used
    /// in a setting where something like a nightly build or other automated process pulls a whole buildable tree to disk.  Once the tree
    /// is in a known good state, as at the end of a build, process automation kicks in and writes aggregate comment and label metadata
    /// into the configured semaphore file.  The change in the timestamp on the semaphore file triggers the flow of information into 
    /// TFS with Comment and Label information inside the file used to decorate the changeset.  The semaphore file takes this form:
    /// 
    ///     <?xml version="1.0"?>
    ///     <Metadata xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
    ///         <ChangeGroup Comment="This comment will be used for the changeset imported to TFS whenever the semaphore file is touched" /> 
    ///         <Label Name="Build 30422.02" Comment="My label comment" />
    ///     </Metadata>
    ///     
    /// </summary>

    public class SemaphoreFileAnalysisAddin : AnalysisAddin
    {
        /// <summary>
        /// The GUID string of the Reference Name of this Add-in
        /// </summary>
        public const string ReferenceNameString = "AC260D98-2411-4829-B4F7-D548F1BC8143";

        private const string c_semaphoreFileHWNName = "HWM_SemaphoreFile";

        // CustomSettingKeyNames
        private const string c_semaphoreFilePath = "SemaphoreFilePath";
        private const string c_excludeDateTimeFromLabelName = "ExcludeDateTimeFromLabelName";

        // Semaphore file XML element and attribute names
        private const string c_changeGroup = "ChangeGroup";
        private const string c_label = "Label";
        private const string c_name = "Name";
        private const string c_comment = "Comment";

        private ConfigurationService m_configurationService;
        private VCTranslationService m_vcTranslationService;
        private Guid m_sourceId;

        private string m_semaphoreFilePath;
        private bool m_excludeDateTimeFromLabelName;
        private HighWaterMark<DateTime> m_hwmSemaphoreFile;
        private DateTime m_semaphoreFileTouchedTime;

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
            get { return SemaphoreFileAnalysisAddinResources.AddinFriendlyName; }
        }


        public override ReadOnlyCollection<string> CustomSettingKeys
        {
            get
            {
                List<string> customSettingKeys = new List<string>();
                customSettingKeys.Add(c_semaphoreFilePath);
                customSettingKeys.Add(c_excludeDateTimeFromLabelName);
                return new ReadOnlyCollection<string>(customSettingKeys);
            }
        }

        public override ReadOnlyCollection<Guid> SupportedMigrationProviderNames
        {
            get 
            {
                List<Guid> supportedMigrationProviders = new List<Guid>();

                // FileSystem2008Adapter
                supportedMigrationProviders.Add(new Guid("3A27F4DE-8637-483C-945D-D2B20541DF7C"));

                // FileSystem2010Adapter
                supportedMigrationProviders.Add(new Guid("43B0D301-9B38-4caa-A754-61E854A71C78"));

                // ClearCase Selected History Adapter for Tfs2008
                supportedMigrationProviders.Add(new Guid("F65A4623-3856-4507-B5E9-AD28811FD37E"));

                // ClearCase Selected History Adapter for Tfs2010
                supportedMigrationProviders.Add(new Guid("B9379F30-2026-4d36-92A6-9654ABF91BFD"));

                return new ReadOnlyCollection<Guid>(supportedMigrationProviders);
            }
        }
        #endregion

        #region AnalysisAddin implementation
        public override void PreAnalysis(AnalysisContext analysisContext)
        {
            m_configurationService = analysisContext.TookitServiceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;

            if (m_configurationService != null)
            {
                // Create a HWM and set the initial value 
                m_hwmSemaphoreFile = new HighWaterMark<DateTime>(c_semaphoreFileHWNName);
                m_configurationService.RegisterHighWaterMarkWithSession(m_hwmSemaphoreFile);

                Dictionary<string, string> addinCustomSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                addinCustomSettings.Add(c_semaphoreFilePath, null);
                addinCustomSettings.Add(c_excludeDateTimeFromLabelName, null);

                AddinCustomSettingsHelper.GetAddinCustomSettings(m_configurationService, ReferenceNameString, addinCustomSettings);
                m_semaphoreFilePath = addinCustomSettings[c_semaphoreFilePath];

                if (string.IsNullOrEmpty(m_semaphoreFilePath))
                {
                    throw new MigrationException(SemaphoreFileAnalysisAddinResources.SemaphoreFilePathNotConfigured);
                }

                if (addinCustomSettings[c_excludeDateTimeFromLabelName] != null && 
                    string.Equals(addinCustomSettings[c_excludeDateTimeFromLabelName], "true", StringComparison.OrdinalIgnoreCase))
                {
                    m_excludeDateTimeFromLabelName = true;
                }
            }
            else
            {
                Debug.Fail("SemaphoreFileAnalysisAddin unable to get ConfigurationService");
                throw new ArgumentNullException("ConfigurationService");
            }

            m_vcTranslationService = analysisContext.TookitServiceContainer.GetService(typeof(ITranslationService)) as VCTranslationService;
            
            m_sourceId = new Guid(m_configurationService.MigrationSource.InternalUniqueId);
        }

        public override bool ProceedToAnalysis(AnalysisContext analysisContext)
        {
            m_semaphoreFileTouchedTime = GetSemaphoreFileTouchedTime();
            m_hwmSemaphoreFile.Reload();

            if (m_semaphoreFileTouchedTime > m_hwmSemaphoreFile.Value)
            {
                // Persisted DateTime is rounded down to the nearest second in the DB.  Add
                // a second to the HWM to make sure we don't take changes due to shaved milliseconds.
                TimeSpan oneSecond = new TimeSpan(0, 0, 1);
                m_hwmSemaphoreFile.Update(m_semaphoreFileTouchedTime + oneSecond);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void PostChangeGroupDeltaComputation(AnalysisContext analysisContext, ChangeGroup changeGroup)
        {
            // Process ChangeGroup element if it exists in semaphore file
            string comment = GetChangeGroupComment(analysisContext.TookitServiceContainer, changeGroup);
            if (!string.IsNullOrEmpty(comment))
            {
                changeGroup.Comment = comment;
            }

            foreach (MappingEntry mapping in m_configurationService.Filters)
            {
                if (!mapping.Cloak && !string.IsNullOrEmpty(mapping.Path))
                {
                    ILabel label = GetSubTreeLabel(analysisContext.TookitServiceContainer, mapping.Path);

                    // Process Label element if it exists in semaphore file
                    if (label != null)
                    {
                        GenerateLabelActionsHelper.AddLabelActionsToChangeGroup(changeGroup, label);
                    }
                }
            }
        }  

        #endregion

        #region private methods
        private DateTime GetSemaphoreFileTouchedTime()
        {
            DateTime semaphoreFileTouchedTime = DateTime.MinValue;


            if (m_semaphoreFilePath != null && File.Exists(m_semaphoreFilePath))
            {
                try
                {
                    // The semaphoreFileTouchedTime is the larger of the file's creation time or the file's last write time
                    // (In the case of a file copy, the creation time is changed but the LastWriteTime is not, and we want
                    // a file copied to the semaphoreFilePath to indicate readiness.)
                    FileInfo fileInfo = new FileInfo(m_semaphoreFilePath);
                    semaphoreFileTouchedTime = (fileInfo.CreationTime > fileInfo.LastWriteTime) ? fileInfo.CreationTime : fileInfo.LastWriteTime;
                }
                catch (Exception ex)
                {
                    TraceManager.TraceError(String.Format(CultureInfo.InvariantCulture,
                        SemaphoreFileAnalysisAddinResources.UnableToReadSemaphoreFile, Path.GetFullPath(m_semaphoreFilePath), ex.ToString()));
                }
            }

            return semaphoreFileTouchedTime;
        }

        /// <summary>
        /// Get the information for a new label to be created on the target side of a migration/sync operation
        /// for item with a specified path.   The path format will be the same as that of the filter pair
        /// strings for the source side adapter.  The label returned is intended to be applied recursively 
        /// to all items in the corresponding sub-tree on the target side.
        /// </summary>
        /// <param name="serviceContainer">A service container that provides access to services provided by the
        /// TFS Integration Platform Toolkit</param>
        /// <param name="subTreePath">The path of a sub tree being migrated (as specified a filter string)</param>
        /// <returns>An object that implements the ILabel interface</returns>
        private ILabel GetSubTreeLabel(IServiceContainer serviceContainer, string fileSystemPath)
        {
            SubTreeLabel label = null;

            try
            {
                XmlElement element = GetSemaphoreFileElement(c_label);

                if (m_vcTranslationService == null)
                {
                    // This Addin is probably configured with a WIT session; ignore any Label element that exists,
                    // but log a warning if one does.
                    if (element != null)
                    {
                        TraceManager.TraceWarning(SemaphoreFileAnalysisAddinResources.LabelInSemaphoreFileIgnored);
                    }
                }
                else
                {
                    // If the Label element is present, we will either take values from the Name and Comment 
                    // attributes or will generate defaults.  If the Label element is not found, we will not
                    // add a label.
                    if (element != null && m_vcTranslationService != null)
                    {
                        string targetSideScope = m_vcTranslationService.GetMappedPath(fileSystemPath, m_sourceId);
                        label = new SubTreeLabel(fileSystemPath, targetSideScope);

                        foreach (XmlAttribute attribute in element.Attributes)
                        {
                            if (string.Equals(attribute.Name, c_name))
                            {
                                label.Name = attribute.Value;
                                if (!m_excludeDateTimeFromLabelName)
                                {
                                    label.Name += "_" + String.Format(CultureInfo.InvariantCulture, SemaphoreFileAnalysisAddinResources.DefaultLabelNameFormat,
                                        DateTime.Now);
                                }
                            }
                            else if (string.Equals(attribute.Name, c_comment))
                            {
                                label.Comment = attribute.Value;
                            }
                        }

                        label.LabelItems.Add(new RecursiveLabelItem(fileSystemPath));
                    }
                    else
                    {
                        TraceManager.TraceInformation(String.Format(CultureInfo.InvariantCulture,
                            SemaphoreFileAnalysisAddinResources.LabelNotFoundInSemaphoreFile,
                            Path.GetFullPath(m_semaphoreFilePath)));
                    }
                }
            }
            catch (Exception ex)
            {
                TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture,
                    SemaphoreFileAnalysisAddinResources.ExceptionProcessingSemaphoreFile,
                    ex.ToString()));
            }

            return label;
        }

        /// <summary>
        /// Get a comment string to be used on the change group on the target side of a migration/sync operation
        /// for the specified change group.
        /// </summary>
        /// <param name="serviceContainer">A service container that provides access to services provided by the
        /// TFS Integration Platform Toolkit</param>
        /// <param name="changeGroup">The change group being migrated for which a label should be generated</param>
        /// <returns>A string that is a comment to be applied to the change group on the target side</returns>       
        private string GetChangeGroupComment(IServiceContainer serviceContainer, ChangeGroup changeGroup)
        {
            string changeGroupComment = null;

            try
            {
                XmlElement element = GetSemaphoreFileElement(c_changeGroup);
                
                if (element != null)
                {
                    foreach (XmlAttribute attribute in element.Attributes)
                    {
                        if (string.Equals(attribute.Name, c_comment))
                        {
                            changeGroupComment = attribute.Value;
                            break;
                        }
                    }

                    // The ChangeGroup element was found but not the Comment attribute.  Generate a Comment.
                    if (changeGroupComment == null)
                    {
                        changeGroupComment = String.Format(CultureInfo.InvariantCulture,
                            SemaphoreFileAnalysisAddinResources.ChangeGroupCommentFormat,
                            changeGroup.Name);
                    }
                }
                else
                {
                    TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture,
                        SemaphoreFileAnalysisAddinResources.ChangeGroupCommentNotFoundInSemaphoreFile,
                        Path.GetFullPath(m_semaphoreFilePath)));
                }
            }
            catch (Exception ex)
            {
                TraceManager.TraceWarning(String.Format(CultureInfo.InvariantCulture,
                    SemaphoreFileAnalysisAddinResources.ExceptionProcessingSemaphoreFile,
                    ex.ToString()));
            }

            return changeGroupComment;
        }

        private XmlElement GetSemaphoreFileElement(string elementName)
        {
            if (m_semaphoreFilePath != null && File.Exists(m_semaphoreFilePath))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(m_semaphoreFilePath);
                foreach (XmlNode node in doc.LastChild.ChildNodes)
                {
                    XmlElement element = node as XmlElement;
                    if (element != null)
                    {
                        if (string.Equals(element.Name, elementName, StringComparison.OrdinalIgnoreCase))
                        {
                            return element;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public override object GetService(Type serviceType)
        {
            if (serviceType == typeof(AnalysisAddin) ||
                serviceType == typeof(SemaphoreFileAnalysisAddin))
            {
                return this;
            }

            return null;
        }

        #endregion
    }
}