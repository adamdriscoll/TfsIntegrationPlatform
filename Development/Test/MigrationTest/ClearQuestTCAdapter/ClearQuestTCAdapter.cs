// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter;
using Microsoft.TeamFoundation.Migration.ClearQuestAdapter.CQInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTestLibrary;

namespace ClearQuestV7TCAdapter
{
    [TCAdapterDescription(m_adapterGuid, m_adapterName)]
    public class ClearQuestTCAdapter : IWITTestCaseAdapter
    {
        private const string m_adapterGuid = "35AF98D6-5227-4807-B205-CD97AF08A1CA";
        private const string m_adapterName = "ClearQuest V7 TestCase Adapter";

        // TODO: remove hard-coded type
        private const string m_workItemType = "Defect";

        private string m_filterString;
        private ClearQuestOleServer.Session m_session;

        public event WorkItemAddedEventHandler WorkItemAdded;

        public string FilterString
        {
            get
            {
                return m_filterString;
            }
        }


        public string TitlePrefix { get; set; }
        public string TitleQuery
        {
            get
            {
                return String.Format("headline CONTAINS \"{0}\"", TitlePrefix);
            }
        }

        public void Initialize(EndPoint env)
        {
            m_filterString = string.Format("{0}::", m_workItemType);

            // TODO: move connection settings to configuration file
            ClearQuestConnectionConfig connection = new ClearQuestConnectionConfig("hykwon", "hykwon", "UCM01", "7.0.0");
            m_session = CQConnectionFactory.GetUserSession(connection);
        }

        public void Cleanup()
        {
        }

        public int AddWorkItem(string type, string title, string description)
        {
            OAdEntity entity = CQWrapper.BuildEntity(m_session, m_workItemType);

            SetFieldValue(entity, "headline", TitlePrefix + " " + title);
            SetFieldValue(entity, "Description", description);
            SetFieldValue(entity, "Severity", "1-Critical");

            SaveWorkItem(entity);

            int dbid = entity.GetDbId();
            if (WorkItemAdded != null)
            {
                WorkItemAdded(this, new WorkItemAddedEventArgs(dbid));
            }

            return dbid;
        }

        public void UpdateWorkItem(int workItemId, WITChangeAction action)
        {
            // find the entity
            OAdEntity entity = GetEntityByDBId(workItemId);

            // mark entity to be editable
            CQWrapper.EditEntity(m_session, entity, "Modify");

            SetFieldValue(entity, "headline", TitlePrefix + " " + action.Title);
            SetFieldValue(entity, "Description", action.Description);
            SetFieldValue(entity, "Note_Entry", action.History);

            // TODO:
            //SetFieldValue(entity, "Reason", action.Reason);
            //SetFieldValue(entity, "Priority", action.Priority);
            //SetFieldValue(entity, "AssignedTo", action.AssignedTo);

            SaveWorkItem(entity);
        }

        public void UpdateWorkItemLink(int workItemId, WITLinkChangeAction action)
        {
            throw new NotImplementedException();
        }

        public string GetFieldValue(int workItemId, string fieldName)
        {
            throw new NotImplementedException();
        }

        public WITLink GetHyperLink(int workItemId, string location)
        {
            throw new NotImplementedException();
        }

        public int GetAttachmentCount(int workItemId)
        {
            throw new NotImplementedException();
        }

        public void UpdateAttachment(int workItemId, WITAttachmentChangeAction action)
        {
            // find the entity
            OAdEntity entity = GetEntityByDBId(workItemId);

            // mark entity to be editable
            CQWrapper.EditEntity(m_session, entity, "Modify");

            foreach (WITAttachment attachment in action.Attachments)
            {
                if (attachment.ActionType == AttachmentChangeActionType.Add)
                {
                    AddAttachment(entity, attachment.FileName, attachment.Comment);
                }
                else if (attachment.ActionType == AttachmentChangeActionType.Delete)
                {
                    throw new NotImplementedException("DeleteAttachment is not supported yet");
                }
                // Update attachment comment
                else if (attachment.ActionType == AttachmentChangeActionType.Edit)
                {
                    throw new NotImplementedException("EditAttachment is not supported yet");
                }
            }

            SaveWorkItem(entity);
        }

        #region private methods
        private void SetFieldValue(OAdEntity entity, string fieldName, string fieldValue)
        {
            if (string.IsNullOrEmpty(fieldValue))
            {
                return;
            }

            string retVal = CQWrapper.SetFieldValue(entity, fieldName, fieldValue);
            Trace.WriteIf(!string.IsNullOrEmpty(retVal), "retVal = " + retVal);
            Assert.IsTrue(string.IsNullOrEmpty(retVal),
                string.Format("SetFiledValue returned non-empty result : {0}, {1}", fieldName, fieldValue));
        }

        private void AddAttachment(OAdEntity entity, string filePath, string comment)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            string path = CreateAttachment(filePath);
            string retVal = CQWrapper.AddAttachmentFieldValue(entity, "Attachments", path, comment);
            Trace.WriteIf(!string.IsNullOrEmpty(retVal), "retVal = " + retVal);
            Assert.IsTrue(string.IsNullOrEmpty(retVal),
                string.Format("AddAttachmentFieldValue returned non-empty result : {0}, {1}", filePath, comment));
        }

        private string CreateAttachment(string filename)
        {
            return TestUtils.CreateRandomFile(Path.Combine(TestUtils.TextReportRoot, filename), 10);
        }

        private void SaveWorkItem(OAdEntity entity)
        {
            string retVal = CQWrapper.Validate(entity);
            Trace.WriteIf(!string.IsNullOrEmpty(retVal), "retVal = " + retVal);
            Assert.IsTrue(string.IsNullOrEmpty(retVal), "retVal is not empty after validate");

            retVal = CQWrapper.Commmit(entity);
            Trace.WriteIf(!string.IsNullOrEmpty(retVal), "retVal = " + retVal);
            Assert.IsTrue(string.IsNullOrEmpty(retVal), "retVal is not empty after commit");
        }

        private OAdEntity GetEntityByDBId(int dbId)
        {
            OAdEntity entity = null;
            try
            {
                entity = (OAdEntity)m_session.GetEntityByDbId(m_workItemType, dbId);
            }
            catch (COMException ex)
            {
                Assert.Fail("GetEntityByDBId failed : {0}", ex.Message);
            }
            return entity;
        }
        #endregion

    }
}
