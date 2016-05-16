// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    public class FileAttachmentOversizedConflictType : ConflictType
    {
        public MigrationConflict CreateConflict(
            string attachmentName,
            string fileSize,
            long maxAttachmentSize,
            string workItemId,
            string serverName,
            string teamProject,
            IMigrationAction conflictedAttachmentAction)
        {
            return new FileAttachmentOversizedConflictType().CreateConflict(
                    CreateConflictDetails(attachmentName, fileSize, maxAttachmentSize, workItemId, serverName, teamProject),
                    FileAttachmentOversizedConflictType.CreateScopeHint(workItemId, attachmentName),
                    conflictedAttachmentAction);
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return FileAttachmentOversizedConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return FileAttachmentOversizedConflictTypeConstants.FriendlyName;
            }
        }

        public override string HelpKeyword
        {
            get
            {
                return "TFSIT_FileAttachmentOversizedConflictType";
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public FileAttachmentOversizedConflictType()
            : base(new FileAttachmentOversizedConflictHandler())
        { }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            foreach (var action in FileAttachmentOversizedConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }

        protected override void RegisterConflictDetailsPropertyKeys()
        {
            foreach (var key in FileAttachmentOversizedConflictTypeConstants.SupportedConflictDetailsPropertyKeys)
            {
                RegisterConflictDetailsPropertyKey(key);
            }
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            try
            {
                ConflictDetailsProperties properties = ConflictDetailsProperties.Deserialize(dtls);

                if (!string.IsNullOrEmpty(properties[FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_AttachmentName]))
                {
                    return string.Format(TfsWITAdapterResources.ErrorAttachmentFileExceedsMaxSize,
                        properties[FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_WorkItemId],
                        properties[FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_AttachmentName],
                        properties[FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_FileSize],
                        properties[FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_MaxAttachmentSize]);
                }
                else
                {
                    // no expected data, just return raw details string
                    return dtls;
                }
            }
            catch (Exception)
            {
                // old style conflict details, just return raw details string
                return dtls;
            }
        }

        /// <summary>
        /// Creates the details of this conflict
        /// </summary>
        /// <param name="attachmentName"></param>
        /// <param name="fileSize"></param>
        /// <param name="maxAttachmentSize"></param>
        /// <param name="workItemId"></param>
        /// <param name="serverName"></param>
        /// <param name="teamProject"></param>
        /// <returns></returns>
        public static string CreateConflictDetails(
            string attachmentName,
            string fileSize,
            long maxAttachmentSize,
            string workItemId,
            string serverName,
            string teamProject)
        {
            ConflictDetailsProperties detailsProperties = new ConflictDetailsProperties();
            detailsProperties.Properties.Add(
                FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_AttachmentName, attachmentName);
            detailsProperties.Properties.Add(
                FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_FileSize, fileSize);
            detailsProperties.Properties.Add(
                FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_MaxAttachmentSize, maxAttachmentSize.ToString());
            detailsProperties.Properties.Add(
                FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_WorkItemId, workItemId);
            detailsProperties.Properties.Add(
                FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_ServerName, serverName);
            detailsProperties.Properties.Add(
                FileAttachmentOversizedConflictTypeConstants.ConflictDetailsKey_TeamProject, teamProject);
            return detailsProperties.ToString();
        }

        /// <summary>
        /// Creates the scope hint of this type of conflict.
        /// /TargetItemId/AttachmentFileName
        /// Note: Target side item Id is expected; it is always available as attachment is always the last changes
        ///       submitted to the target server.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string CreateScopeHint(string targetItemId, string attachmentFileName)
        {
            return string.Format("/{0}/{1}", targetItemId, attachmentFileName);
        }
    }
}
