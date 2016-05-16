// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Xml;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Proxy;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    /// <summary>
    /// Batch update helper; used to submit multiple updates in a single batch.
    /// </summary>
    public class TfsBatchUpdateHelper
    {
        /// <summary>
        /// Submits updates into the work item store.
        /// </summary>
        /// <param name="core">Target TFS</param>
        /// <param name="svc">Work item tracking service</param>
        /// <param name="updates">Updates to submit</param>
        /// <returns>Results</returns>
        public static UpdateResult[] Submit(
            TfsCore core,
            ITfsWorkItemServer svc,
            XmlDocument[] updates)
        {
            TraceManager.EnterMethod(core, svc, updates);

            TfsBatchUpdateHelper helper = new TfsBatchUpdateHelper(core, svc, updates);
            helper.Submit(0, updates.Length - 1);
            return helper.m_results;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="core">Target TFS</param>
        /// <param name="svc">Work item tracking service</param>
        /// <param name="updates">Updates to submit</param>
        private TfsBatchUpdateHelper(
            TfsCore core,
            ITfsWorkItemServer svc,
            XmlDocument[] updates)
        {
            m_core = core;
            m_svc = svc;
            m_updates = updates;
            m_results = new UpdateResult[updates.Length];
        }

        /// <summary>
        /// Submits items from the given range.
        /// </summary>
        /// <param name="firstItem">First item in the range</param>
        /// <param name="lastItem">Last item in the range</param>
        private void Submit(
            int firstItem,
            int lastItem)
        {
            Debug.Assert(firstItem <= lastItem, "Invalid range!");
            XmlDocument doc = new XmlDocument();
            XmlElement packageElement = doc.CreateElement("Package");
            packageElement.SetAttribute("Product", m_core.WorkItemTrackingUrl);
            doc.AppendChild(packageElement);

            for (int i = firstItem; i <= lastItem; i++)
            {
                XmlDocument u = m_updates[i];
                SubmitData(doc, u);
            }

            // Submit the update package
            XmlElement outElement = null;
            Exception resException = null;

            try
            {
                MetadataTableHaveEntry[] metadataHave = new MetadataTableHaveEntry[0];
                string dbStamp;
                IMetadataRowSets rowsets;

                m_svc.Update(
                    m_svc.NewRequestId(),
                    packageElement,
                    out outElement,
                    metadataHave,
                    out dbStamp,
                    out rowsets);
            }
            catch (Exception e)
            {
                resException = e;
            }

            if (resException != null)
            {
                //$TODO_VNEXT: extract more information from SOAP exceptions
                if (firstItem == lastItem)
                {
                    m_results[firstItem] = new UpdateResult(resException);
                }
                else
                {
                    int mid = firstItem + (lastItem - firstItem) / 2;
                    Submit(firstItem, mid);
                    Submit(mid + 1, lastItem);
                }
            }
            else
            {
                // Process results
                foreach (XmlElement resElement in outElement.ChildNodes)
                {
                    int revision = 0;
                    if (resElement.Name == "InsertWorkItem" || resElement.Name == "UpdateWorkItem")
                    {
                        revision = XmlConvert.ToInt32(resElement.GetAttribute("Revision"));
                    }

                    Watermark wm = new Watermark(resElement.GetAttribute("ID"), revision);
                    m_results[firstItem++] = new UpdateResult(wm);
                }
            }
        }

        private void SubmitData(XmlDocument packageDoc, XmlDocument updateOperation)
        {
            XmlElement e = (XmlElement)packageDoc.ImportNode(updateOperation.DocumentElement, true);
            packageDoc.DocumentElement.AppendChild(e);
        }

        private TfsCore m_core;                 // TFS core
        private ITfsWorkItemServer m_svc;            // Work item tracking service
        private XmlDocument[] m_updates;        // Updates to submit
        private UpdateResult[] m_results;       // Update results
    }
}
