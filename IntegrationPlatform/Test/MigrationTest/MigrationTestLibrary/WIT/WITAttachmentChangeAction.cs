// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;

namespace MigrationTestLibrary
{
    public enum AttachmentChangeActionType
    {
        Add,
        Delete,
        Edit
    }

    public class WITAttachmentChangeAction
    {
        private List<WITAttachment> m_attachments = new List<WITAttachment>();

        public WITAttachmentChangeAction()
        {
        }

        public List<WITAttachment> Attachments
        {
            get
            {
                return m_attachments;
            }
        }

        public void AddAttachment(WITAttachment attachment)
        {
            attachment.ActionType = AttachmentChangeActionType.Add;
            m_attachments.Add(attachment);
        }

        public void EditAttachment(WITAttachment attachment)
        {
            attachment.ActionType = AttachmentChangeActionType.Edit;
            m_attachments.Add(attachment);
        }

        public void DeleteAttachment(WITAttachment attachment)
        {
            attachment.ActionType = AttachmentChangeActionType.Delete;
            m_attachments.Add(attachment);
        }
    }
}
