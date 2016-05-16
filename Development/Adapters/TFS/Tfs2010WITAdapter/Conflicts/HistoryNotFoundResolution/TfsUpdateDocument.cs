// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Hist = Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts.HistoryNotFoundResolution;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public partial class TfsUpdateDocument
    {
        internal void AddFields(
            Hist.MigrationAction action,
            string workItemType,
            string author,
            string changedTime,
            bool insertWorkItem)
        {
            bool hasArea = false;
            bool hasIteration = false;
            XmlDocument desc = action.RecordDetails.DetailsDocument;

            XmlElement cs = UpdateDocument.CreateElement("Columns");
            UpdateDocument.FirstChild.AppendChild(cs);
            XmlNodeList columns = desc.SelectNodes("/WorkItemChanges/Columns/Column");

            foreach (XmlNode columnData in columns)
            {
                string fieldName = columnData.Attributes["DisplayName"].Value;
                string fieldReferenceName = columnData.Attributes["ReferenceName"].Value;
                string stringVal = columnData.FirstChild.InnerText;
                string fieldType = columnData.Attributes["Type"].Value;

                if (fieldReferenceName.Equals(CoreFieldReferenceNames.AreaPath)
                    || fieldReferenceName.Equals(CoreFieldReferenceNames.IterationPath))
                {
                    try
                    {
                        GetCSSNodeId(action, workItemType,
                                     ref fieldName, ref fieldReferenceName, ref stringVal, ref hasIteration, ref hasArea); ;
                    }
                    catch (Exception ex)
                    {
                        TraceManager.TraceError(ex.ToString());
                        continue;
                    }
                }

                AddColumn(cs, workItemType, fieldName, fieldReferenceName, stringVal);

            }

            if (insertWorkItem)
            {
                if (!hasArea)
                //if (!hasArea && !ByPassrules)
                {
                    AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.AreaId,
                              MigrationWorkItemStore.Core.DefaultAreaId.ToString());
                }
                if (!hasIteration)
                //if (!hasIteration && !ByPassrules)
                {
                    AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.IterationId,
                              MigrationWorkItemStore.Core.DefaultIterationId.ToString());
                }

                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.WorkItemType, workItemType);
                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.CreatedDate, string.Empty);
                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.CreatedBy, author);
                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.ChangedBy, author);
            }
            else
            {
                AddColumn(cs, workItemType, string.Empty, CoreFieldReferenceNames.ChangedBy, author);
            }
        }

        private void GetCSSNodeId(
            Hist.MigrationAction action,
            string workItemType,
            ref string fieldName,
            ref string fieldReferenceName,
            ref string stringVal,
            ref bool hasIteration,
            ref bool hasArea)
        {
            string fieldNameBeforeConversion = fieldName;
            string fieldRefNameBeforeConversion = fieldReferenceName;

            if (fieldRefNameBeforeConversion.Equals(CoreFieldReferenceNames.AreaPath))
            {
                // Substitute AreaPath with AreaId
                fieldName = string.Empty;
                fieldReferenceName = CoreFieldReferenceNames.AreaId;
                stringVal = MigrationWorkItemStore.Core.TranslatePath(Node.TreeType.Area, stringVal).ToString();
                hasArea = true;
            }
            else if (fieldRefNameBeforeConversion.Equals(CoreFieldReferenceNames.IterationPath))
            {
                // Substitute IterationPath with IterationId
                fieldName = string.Empty;
                fieldReferenceName = CoreFieldReferenceNames.IterationId;
                stringVal = MigrationWorkItemStore.Core.TranslatePath(Node.TreeType.Iteration, stringVal).ToString();
                hasIteration = true;
            }
        }

    }
}
