// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Xml;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace FilePropertiesAnalysisAddin
{
    /// <summary>
    /// This is a sample of AnalysisAddin for the source side of a version control migration or sync that generates migration instructions to add
    /// properties to the files and folders on the target side of the migration.
    /// This sample adds two properties to each folder and file changed in each ChangeGroup migrated (except for deleted items) with the following names:
    ///     "SourceChangeGroupId": The Id of the ChangeGroup on the source side that resulted in the change on the target side.
    ///     "SourceChangeGroupOwner": The owner of that ChangeGroup on the source side (typically the user that performed the check-in)
    /// Note that the target side version control system must support file properties (sometime named "attributes") for this Addin to 
    /// be useful.  TFS 2010 and ClearCase are two version control systems that provide this support.  TFS 2008 does not.
    /// </summary>
    public class FilePropertiesAnalysisAddin : AnalysisAddin
    {
        /// <summary>
        /// The GUID string of the Reference Name of this Add-in
        /// </summary>
        public const string ReferenceNameString = "A745F890-162F-40f4-A445-A0DFBEBF1A1A";

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
            get { return FilePropertiesAnalysisAddinResources.AddinFriendlyName; }
        }

        #endregion

        #region AnalysisAddin implementation
        public override void PostChangeGroupDeltaComputation(AnalysisContext analysisContext, ChangeGroup changeGroup)
        {
            AddFilePropertiesToChangeGroup(analysisContext.TookitServiceContainer, changeGroup);
        }
        #endregion

        #region private methods

        /// <summary>
        /// This method allows the implementing class to add file property metadata for any files
        /// that are added or changed during a migration/sync session.
        /// </summary>
        /// <param name="serviceContainer">A service container that provides access to services provided by the
        /// TFS Integration Platform Toolkit</param>
        /// <param name="changeGroup">The change group being migrated for file property metadata can be generated 
        /// by adding FileProperty change actions to the ChangeGroup</param>        
        private void AddFilePropertiesToChangeGroup(IServiceContainer serviceContainer, ChangeGroup changeGroup)
        {
            ChangeGroupService changeGroupService = (ChangeGroupService)serviceContainer.GetService(typeof(ChangeGroupService));

            // This FilePropertiesAnalysisAddin adds two single properties to each item:
            //      The change group identifier from the source from which is was migrated
            //      The owner of the change group
            FileMetadataProperties fileProperties = new FileMetadataProperties();
            fileProperties.Add(FilePropertiesAnalysisAddinResources.SourceChangeGroupIdKey, changeGroup.Name);
            fileProperties.Add(FilePropertiesAnalysisAddinResources.SourceChangeGroupOwnerKey, changeGroup.Owner);
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
        public override object GetService(Type serviceType)
        {
            if (serviceType == typeof(FilePropertiesAnalysisAddin) ||
                serviceType == typeof(AnalysisAddin))
            {
                return this;
            }

            return null;
        }

        #endregion
    }
}
