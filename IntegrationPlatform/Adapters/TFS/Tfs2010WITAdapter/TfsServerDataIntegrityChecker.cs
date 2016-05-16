// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// This class checks whether a migration document meets the basic data integrity requirements
    /// of TFS server.
    /// </summary>
    /// <remarks>
    /// -- Pick the rev rule (new Rev = old rev + 1)
    /// -- Work Item Type value cannot be empty 
    /// -- State value cannot be empty 
    /// -- Reason value cannot be empty 
    /// -- Changed Date value cannot be empty 
    /// -- AreaId value cannot be empty
    /// -- IterationId value cannot be a deleted tree location
    /// -- AreaId value cannot be a deleted tree location
    /// -- Created Date value cannot be empty
    /// -- Created By value cannot be empty
    /// -- Changed Date must be greater than previous Changed Date.
    /// </remarks>
    internal class TfsServerDataIntegrityChecker
    {
        private const string UnknownFieldValue = "Unknown";
        private Dictionary<string, string> m_mandatoryFields = new Dictionary<string, string>();
        private List<string> m_fieldUpdateList = new List<string>();

        public TfsServerDataIntegrityChecker()
        {
            m_mandatoryFields[CoreFieldReferenceNames.WorkItemType] = string.Empty;
            m_mandatoryFields[CoreFieldReferenceNames.State] = UnknownFieldValue;
            m_mandatoryFields[CoreFieldReferenceNames.Reason] = UnknownFieldValue;
            //m_mandatoryFields[CoreFieldReferenceNames.ChangedDate] = string.Empty; // changed date is updated on server
            m_mandatoryFields[CoreFieldReferenceNames.AreaId] = string.Empty;
            m_mandatoryFields[CoreFieldReferenceNames.IterationId] = string.Empty;
            m_mandatoryFields[CoreFieldReferenceNames.CreatedDate] = string.Empty;
            m_mandatoryFields[CoreFieldReferenceNames.CreatedBy] = string.Empty;

            m_fieldUpdateList.AddRange(m_mandatoryFields.Keys.AsEnumerable());
        }

        internal void RecordUpdatedField(string fieldReferenceName)
        {
            string updatedMandatoryField = null;
            foreach (var field in m_fieldUpdateList)
            {
                if (TFStringComparer.WorkItemFieldReferenceName.Equals(field, fieldReferenceName))
                {
                    updatedMandatoryField = field;
                    break;
                }
            }

            if (null != updatedMandatoryField)
            {
                m_fieldUpdateList.Remove(updatedMandatoryField);
            }
        }

        public string[] MissingFields
        {
            get
            {
                return m_fieldUpdateList.ToArray();
            }
        }

        /// <summary>
        /// Gets the suggested value for mandatory field
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public string this[string field]
        {
            get
            {
                if (m_mandatoryFields.ContainsKey(field))
                {
                    return m_mandatoryFields[field];
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        internal void InitializeForUpdateAnalysis(string workItemType, string author)
        {
            m_mandatoryFields[CoreFieldReferenceNames.WorkItemType] = workItemType;
            m_mandatoryFields[CoreFieldReferenceNames.CreatedBy] = author;
        }
    }
}
