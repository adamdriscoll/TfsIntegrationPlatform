// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    public class WorkItemTypeNotExistConflictType : ConflictType
    {
        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return WorkItemTypeNotExistConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return WorkItemTypeNotExistConflictTypeConstants.FriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_WorkItemTypeNotExistConflictType";
            }
        }

        public WorkItemTypeNotExistConflictType()
            : base(new WorkItemTypeNotExistConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            foreach (var action in WorkItemTypeNotExistConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }

        protected override void RegisterConflictDetailsPropertyKeys()
        {
            foreach (var key in WorkItemTypeNotExistConflictTypeConstants.SupportedConflictDetailsPropertyKeys)
            {
                RegisterConflictDetailsPropertyKey(key);
            }
        }

        public static string CreateConflictDetails(
            Project teamProject,
            string workItemType)
        {
            WorkItemTypeNotExistConflictTypeDetails dtls =
                new WorkItemTypeNotExistConflictTypeDetails(teamProject, workItemType);

            return dtls.Properties.ToString();
        }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /TeamProject/WorkItemType/Field
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string CreateScopeHint(Project teamProject, string workItemType)
        {
            if (null == teamProject)
            {
                throw new ArgumentNullException("teamProject");
            }

            if (string.IsNullOrEmpty(workItemType))
            {
                throw new ArgumentNullException("workItemType");
            }

            return string.Format("/{0}/{1}", teamProject.Name, workItemType);
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            WorkItemTypeNotExistConflictTypeDetails details = null;
            try
            {
                ConflictDetailsProperties properties = ConflictDetailsProperties.Deserialize(dtls);
                details = new WorkItemTypeNotExistConflictTypeDetails(properties);
            }
            catch (Exception)
            {
                try
                {
                    GenericSerializer<WorkItemTypeNotExistConflictTypeDetails> serializer =
                        new GenericSerializer<WorkItemTypeNotExistConflictTypeDetails>();
                    details = serializer.Deserialize(dtls);
                }
                catch (Exception)
                {
                    // do nothing, fall back to raw string later
                }
            }

            if (null != details)
            {
                return string.Format(
                    "Work Item Type {0} is not defined for team project '{1}' on server '{2}'.",
                    details.MissingWorkItemType,
                    details.TeamProject,
                    details.TeamFoundationServer);
            }
            else
            {
                return dtls;
            }
        }
    }

    [Serializable]
    public class WorkItemTypeNotExistConflictTypeDetails
    {
        public WorkItemTypeNotExistConflictTypeDetails()
        { }

        public WorkItemTypeNotExistConflictTypeDetails(Project teamProject, string workItemType)
        {
            Initialize(teamProject.Store.TeamProjectCollection.Name, teamProject.Name, workItemType);
        }

        internal WorkItemTypeNotExistConflictTypeDetails(ConflictDetailsProperties detailsProperties)
        {
            string tfsName, teamProjectName, workItemType;
            if (detailsProperties.Properties.TryGetValue(
                    WorkItemTypeNotExistConflictTypeConstants.ConflictDetailsKey_TeamFoundationServer, out tfsName)
                && detailsProperties.Properties.TryGetValue(
                    WorkItemTypeNotExistConflictTypeConstants.ConflictDetailsKey_TeamProject, out teamProjectName)
                && detailsProperties.Properties.TryGetValue(
                    WorkItemTypeNotExistConflictTypeConstants.ConflictDetailsKey_MissingWorkItemType, out workItemType))
            {
                Initialize(tfsName, teamProjectName, workItemType);
            }
            else
            {
                throw new ArgumentException("detailsProperties do not contain all expected values for the conflict type");
            }
        }

        private void Initialize(string tfsName, string teamProjectName, string workItemType)
        {
            TeamFoundationServer = tfsName;
            TeamProject = teamProjectName;
            MissingWorkItemType = workItemType;
        }

        public string TeamFoundationServer { get; set; }
        public string TeamProject { get; set; }
        public string MissingWorkItemType { get; set; }

        [XmlIgnore]
        public ConflictDetailsProperties Properties
        {
            get
            {
                ConflictDetailsProperties detailsProperties = new ConflictDetailsProperties();
                detailsProperties.Properties.Add(
                    WorkItemTypeNotExistConflictTypeConstants.ConflictDetailsKey_TeamFoundationServer,
                    this.TeamFoundationServer);
                detailsProperties.Properties.Add(
                    WorkItemTypeNotExistConflictTypeConstants.ConflictDetailsKey_TeamProject,
                    this.TeamProject);
                detailsProperties.Properties.Add(
                    WorkItemTypeNotExistConflictTypeConstants.ConflictDetailsKey_MissingWorkItemType,
                    this.MissingWorkItemType);

                return detailsProperties;

            }
        }
    }
}
