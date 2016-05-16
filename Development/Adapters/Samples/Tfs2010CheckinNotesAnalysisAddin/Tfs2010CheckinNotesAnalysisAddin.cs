// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Tfs2010CheckinNotesAnalysisAddin
{
    /// <summary>
    /// A sample implementation FileProperties Addin that demonstrates how an Addin can retrieve metadata from TFS
    /// </summary>
    public class Tfs2010CheckinNotesAnalysisAddin : AnalysisAddin
    {
        /// <summary>
        /// The GUID string of the Reference Name of this Add-in
        /// </summary>
        public const string ReferenceNameString = "F55AA5CF-AF17-438b-9D25-7B6A43346F62";

        private VersionControlServer m_tfsClient;

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
                return Tfs2010CheckinNotesAnalysisAddinResources.AddinFriendlyName;
            }
        }

        #endregion

        #region AnalysisAddin implementation

        public override System.Collections.ObjectModel.ReadOnlyCollection<Guid> SupportedMigrationProviderNames
        {
            get
            {
                List<Guid> supportedMigrationProviders = new List<Guid>();

                // TFS 2010 VC Adapter
                supportedMigrationProviders.Add(new Guid("FEBC091F-82A2-449e-AED8-133E5896C47A"));

                return supportedMigrationProviders.AsReadOnly();   
            }
        }

        public override void PostChangeGroupDeltaComputation(AnalysisContext analysisContext, ChangeGroup changeGroup)
        {
            AddCheckinNotesFilePropertiesToChangeGroup(analysisContext.TookitServiceContainer, changeGroup);
        }

        /// <summary>
        /// This method allows the implementing class to add file property metadata for any files
        /// that are added or changed during a migration/sync session.
        /// </summary>
        /// <param name="serviceContainer">A service container that provides access to services provided by the
        /// TFS Integration Platform Toolkit</param>
        /// <param name="changeGroup">The change group being migrated for file property metadata can be generated 
        /// by adding FileProperty change actions to the ChangeGroup</param>        
        private void AddCheckinNotesFilePropertiesToChangeGroup(IServiceContainer serviceContainer, ChangeGroup changeGroup)
        {
            // Add the changeset number as a file property
            FileMetadataProperties fileProperties = new FileMetadataProperties();

            // Try to parse the changeGroup.Name property to the TFS changeset ID. 
            // This is true in most cases but not all
            int tfsChangesetId;
            if (int.TryParse(changeGroup.Name, out tfsChangesetId))
            {
                try
                {
                    Changeset changeset = GetTfsClient(serviceContainer).GetChangeset(tfsChangesetId);
                    if (changeset.CheckinNote != null)
                    {
                        foreach (CheckinNoteFieldValue checkinNote in changeset.CheckinNote.Values)
                        {
                            string propertyName = MakeSimplePropertyName(checkinNote.Name);
                            fileProperties.Add(propertyName, checkinNote.Value);
                        }
                    }
                }
                catch
                {
                }
            }

            XmlDocument filePropertiesXmlDoc = fileProperties.ToXmlDocument();

            IMigrationAction[] currentActions = new IMigrationAction[changeGroup.Actions.Count];
            changeGroup.Actions.CopyTo(currentActions, 0);
            foreach (IMigrationAction action in currentActions)
            {
                if (string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlledFile.ReferenceName, StringComparison.Ordinal) ||
                    string.Equals(action.ItemTypeReferenceName, WellKnownContentType.VersionControlledFolder.ReferenceName, StringComparison.Ordinal))
                {
                    if (action.Action == WellKnownChangeActionId.Add ||
                        action.Action == WellKnownChangeActionId.Edit ||
                        action.Action == WellKnownChangeActionId.Rename ||
                        action.Action == WellKnownChangeActionId.Undelete)
                    {
                        IMigrationAction addFilePropertiesAction = changeGroup.CreateAction(
                            WellKnownChangeActionId.AddFileProperties,
                            action.SourceItem,
                            action.FromPath,
                            action.Path,
                            action.Version,
                            null,
                            action.ItemTypeReferenceName,
                            filePropertiesXmlDoc);
                    }
                }
            }
        }
        #endregion

        #region IServiceProvider Members

        /// <summary>
        /// Gets the service object of the specified type
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(AnalysisAddin) ||
                serviceType == typeof(Tfs2010CheckinNotesAnalysisAddin))
            {
                return this;
            }

            return null;
        }

        #endregion

        #region Private Methods
        private VersionControlServer GetTfsClient(IServiceContainer serviceContainer)
        {
            if (m_tfsClient == null)
            {
                // Get the TFS VersionControlServer proxy object used to get version control information from TFS
                m_tfsClient = (VersionControlServer)serviceContainer.GetService(typeof(VersionControlServer));
            }
            return m_tfsClient;
        }

        /// <summary>
        /// Convert an arbitrary name into a simpler property name that uses a lowest common denominator for valid
        /// property name characters (this happens to match the ClearCase naming rules which are quite restrictive).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string MakeSimplePropertyName(string name)
        {
            string specialClearCaseNameChars = "_-.";
            char [] nameChars = name.ToCharArray();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char nameChar = nameChars[i];
                // Only include character from the original name that are valid ClearCase name characters
                // with are letters, digits, and the special characters underscore (_), period
                // (.), and hyphen (-)
                if (Char.IsLetterOrDigit(nameChar) || specialClearCaseNameChars.IndexOf(nameChar) != -1)
                {
                    sb.Append(nameChar);
                }
            }
            return sb.ToString();
        }
        #endregion
    }
}
