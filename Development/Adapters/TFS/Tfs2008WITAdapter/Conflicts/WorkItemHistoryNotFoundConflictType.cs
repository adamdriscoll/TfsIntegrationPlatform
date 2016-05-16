// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter.Conflicts
{
    /// <summary>
    /// The TFS WIT history-not-found conflict type.
    /// </summary>
    public class WorkItemHistoryNotFoundConflictType : ConflictType
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public WorkItemHistoryNotFoundConflictType()
            : base(new WorkItemHistoryNotFoundConflictHandler())
        {
        }
        
        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return WorkItemHistoryNotFoundConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return WorkItemHistoryNotFoundConflictTypeConstants.FriendlyName;
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            foreach (var action in WorkItemHistoryNotFoundConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }

        /// <summary>
        /// Creates a new conflict of this type
        /// </summary>
        /// <param name="sourceItemId"></param>
        /// <param name="sourceItemRevision"></param>
        /// <param name="conflictedAction"></param>
        /// <returns></returns>
        public static MigrationConflict CreateConflict(
            string sourceItemId,
            string sourceItemRevision,
            Guid sourceMigrationSourceId,
            Guid targetMigrationSourceId,
            IMigrationAction conflictedAction)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceItemId), "sourceItemId is Null or Empty");
            Debug.Assert(!string.IsNullOrEmpty(sourceItemRevision), "sourceItemRevision is Null or Empty");

            string conflictDetails = CreateConflictDetails(sourceItemId, sourceItemRevision, sourceMigrationSourceId, targetMigrationSourceId);
            string scopeHint = CreateScopeHint(sourceItemId, sourceItemRevision);

            MigrationConflict conflict = new MigrationConflict(
                new WorkItemHistoryNotFoundConflictType(), 
                MigrationConflict.Status.Unresolved, 
                conflictDetails,
                scopeHint);

            conflict.ConflictedChangeAction = conflictedAction;

            return conflict;
        }

        /// <summary>
        /// Translates the conflict details to a readable description.
        /// </summary>
        /// <param name="dtls"></param>
        /// <returns></returns>
        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(WorkItemHistoryNotFoundConflictTypeDetails));
            using (StringReader strReader = new StringReader(dtls))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                WorkItemHistoryNotFoundConflictTypeDetails details =
                    (WorkItemHistoryNotFoundConflictTypeDetails)serializer.Deserialize(xmlReader);

                return string.Format(
                    "Work Item conversion history for Source Item '{0}' (revision: '{1}', Migration Source: '{2}') is not found.",
                    details.SourceWorkItemID, details.SourceWorkItemRevision, details.SourceMigrationSourceId.ToString());
            }
        }

        internal WorkItemHistoryNotFoundConflictTypeDetails GetConflictDetails(
            MigrationConflict conflict)
        {
            if (!conflict.ConflictType.ReferenceName.Equals(this.ReferenceName))
            {
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(conflict.ConflictDetails))
            {
                throw new ArgumentNullException("conflict.ConflictDetails");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(WorkItemHistoryNotFoundConflictTypeDetails));

            using (StringReader strReader = new StringReader(conflict.ConflictDetails))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                return serializer.Deserialize(xmlReader) as WorkItemHistoryNotFoundConflictTypeDetails;
            }
        }

        private static string CreateConflictDetails(
            string sourceItemId,
            string sourceItemRevision,
            Guid sourceMigrationSourceId,
            Guid targetMigrationSourceId)
        {
            WorkItemHistoryNotFoundConflictTypeDetails dtls =
               new WorkItemHistoryNotFoundConflictTypeDetails(sourceItemId, sourceItemRevision, sourceMigrationSourceId, targetMigrationSourceId);

            XmlSerializer serializer = new XmlSerializer(typeof(WorkItemHistoryNotFoundConflictTypeDetails));
            using (MemoryStream memStrm = new MemoryStream())
            {
                serializer.Serialize(memStrm, dtls);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }

        private static string CreateScopeHint(
            string sourceItemId,
            string sourceItemRevision)
        {
            return string.Format("/{0}/{1}", sourceItemId, sourceItemRevision);
        }
    }

    /// <summary>
    /// The serializable details class of the TFS WIT history-not-found conflict type
    /// </summary>
    [Serializable]
    public class WorkItemHistoryNotFoundConflictTypeDetails
    {
        /// <summary>
        /// Default constructor needed by serialization.
        /// </summary>
        public WorkItemHistoryNotFoundConflictTypeDetails()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sourceItemId">Source work item Id</param>
        /// <param name="sourceItemRevision">Source work item revision number</param>
        /// <param name="sourceMigrationSourceId">Target work item Id</param>
        /// <param name="targetMigrationSourceId">Target work item revision number</param>
        public WorkItemHistoryNotFoundConflictTypeDetails(
            string sourceItemId,
            string sourceItemRevision,
            Guid sourceMigrationSourceId,
            Guid targetMigrationSourceId)
        {
            SourceWorkItemID = sourceItemId;
            SourceWorkItemRevision = sourceItemRevision;
            SourceMigrationSourceId = sourceMigrationSourceId;
            TargetMigrationSourceId = targetMigrationSourceId;
        }

        /// <summary>
        /// Gets the source work item Id
        /// </summary>
        public string SourceWorkItemID { get; set; }

        /// <summary>
        /// Gets the source work item revision number
        /// </summary>
        public string SourceWorkItemRevision { get; set; }

        /// <summary>
        /// Gets the target work item Id
        /// </summary>
        public Guid SourceMigrationSourceId { get; set; }

        /// <summary>
        /// Gets the target work item revision number
        /// </summary>
        public Guid TargetMigrationSourceId { get; set; }
    }
}

