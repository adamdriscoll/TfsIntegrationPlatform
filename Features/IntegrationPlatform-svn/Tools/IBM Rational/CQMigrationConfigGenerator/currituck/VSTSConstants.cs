// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: VSTS Constants class

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking
{
    internal enum ConverterSource
    {
        MIN = 0,
        PS = MIN,
        CQ = 1,
        MAX = CQ + 1
    }

    internal static class VSTSConstants
    {
        /*
         * fix for bug#11206
         * there is additional constraint that some of the core fields names are internal
         * and cannot be used by the user... for temporary solution listing out all those 
         * core fields.. shall go out once there is some better solution from currituck
         */
        private static Dictionary<string, bool> m_tfsInternalFields;
        internal static Dictionary<string, bool> TfsInternalFields
        {
            get
            {
                if (m_tfsInternalFields == null)
                {
                    m_tfsInternalFields = new Dictionary<string, bool>(TFStringComparer.WorkItemFieldReferenceName);

                    // add all the internal field ref names from Currituck
                    foreach (string internalRefName in InternalFields.RefNamesAll)
                    {
                        m_tfsInternalFields.Add(internalRefName, false);
                    }

                    // now manually add the field names as well
                    string[] hiddenFields = { "attached files", 
                        "changed set", 
                        "inadminonlytreeflag",
                        "indeletedtreeflag",
                        "linked files",
                        "node type",
                        "not a field",
                        "personid",
                        "projectid",
                        "related links",
                        "tf server",
                        "tree",
                        "work item form",
                        "work item formid",
                        "workitem",
                        "area level 1",
                        "area level 2",
                        "area level 3",
                        "area level 4",
                        "area level 5",
                        "area level 6",
                        "area level 7",
                        "iteration level 1",
                        "iteration level 2",
                        "iteration level 3",
                        "iteration level 4",
                        "iteration level 5",
                        "iteration level 6",
                        "iteration level 7",
                        "bis links" };
                    foreach (string hiddenField in hiddenFields)
                    {
                        m_tfsInternalFields.Add(hiddenField, false);
                    }
                }

                return m_tfsInternalFields;
            }
        }

        //originally defined in vset\scm\workitemtracking\common\psdbdal.ch
        internal const string BisProjectPrepend = @"$Project:";
        internal const string HistoryFieldRefName = @"System.History";
        internal const string AreaIdFieldRefName = @"System.AreaId";
        internal const string IterationIdFieldRefName = @"System.IterationId";
        internal const string WorkItemTypeFieldRefName = @"System.WorkItemType";
        internal const string CreatedDateFieldRefName = @"System.CreatedDate";
        internal const string CreatedByFieldRefName = @"System.CreatedBy";
        internal const string StateFieldRefName = @"System.State";
        internal const string ReasonFieldRefName = @"System.Reason";
        internal const string ChangedDateFieldRefName = @"System.ChangedDate";
        internal static string MigrationStatusField = String.Empty;

        // following field names are fetched from TFS sevrer
        // as they may be localized
        private static string m_AreaPathField;
        internal static string AreaPathField
        {
            get
            {
                if (m_AreaPathField == null)
                {
                    m_AreaPathField = VstsConn.store.FieldDefinitions["System.AreaPath"].Name;
                }
                return m_AreaPathField;
            }
        }

        private static string m_IterationPathField;
        internal static string IterationPathField
        {
            get
            {
                if (m_IterationPathField == null)
                {
                    m_IterationPathField = VstsConn.store.FieldDefinitions["System.IterationPath"].Name;
                }
                return m_IterationPathField;
            }
        }

        private static string m_AreaIdField;
        internal static string AreaIdField
        {
            get
            {
                if (m_AreaIdField == null)
                {
                    m_AreaIdField = VstsConn.store.FieldDefinitions["System.AreaId"].Name;
                }
                return m_AreaIdField;
            }
        }

        private static string m_IterationIdField;
        internal static string IterationIdField
        {
            get
            {
                if (m_IterationIdField == null)
                {
                    m_IterationIdField = VstsConn.store.FieldDefinitions["System.IterationId"].Name;
                }
                return m_IterationIdField;
            }
        }

        private static string m_DescriptionField;
        internal static string DescriptionField
        {
            get
            {
                if (m_DescriptionField == null)
                {
                    m_DescriptionField = VstsConn.store.FieldDefinitions["System.Description"].Name;
                }
                return m_DescriptionField;
            }
        }

        internal const int MaxStringFieldLength = 255;
        
        private static string m_CurrentDate;
        internal static string CurrentDate
        {
            get
            {
                if (m_CurrentDate == null)
                {
                    m_CurrentDate = Microsoft.TeamFoundation.Converters.WorkItemTracking.Common.CommonConstants.ConvertDateToString(TimeZone.CurrentTimeZone.ToUniversalTime(DateTime.Now));
                }
                return m_CurrentDate;
            }
        }

        // fields to be skipped while building initial snapshot
        private static Dictionary<string, bool> m_skipFields;
        internal static Dictionary<string, bool> SkipFields
        {
            get
            {
                if (m_skipFields == null)
                {
                    // initialize while first call
                    m_skipFields = new Dictionary<string, bool>(TFStringComparer.WIConverterFieldRefName);

                    // first add all the inrernal fields which are not visible
                    // using OM but are disallowed to be used by end customer
                    foreach (string internalFld in TfsInternalFields.Keys)
                    {
                        m_skipFields.Add(internalFld, false);
                    }

                    // internal field ref names                    
                    m_skipFields.Add("System.AttachedFileCount", false);
                    m_skipFields.Add("System.AuthorizedAs", false);
                    m_skipFields.Add("System.ChangedDate", false);
                    m_skipFields.Add("System.ExternalLinkCount", false);
                    m_skipFields.Add("System.HyperLinkCount", false);
                    m_skipFields.Add("System.Id", false);
                    m_skipFields.Add("System.IterationPath", false);
                    m_skipFields.Add("System.NodeName", false);
                    m_skipFields.Add("System.RelatedLinkCount", false);
                    m_skipFields.Add("System.Rev", false);
                    m_skipFields.Add("System.TeamProject", false);
                    m_skipFields.Add("System.RevisedDate", false);

                    // internal field names                    
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.AttachedFileCount"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.AuthorizedAs"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.ChangedDate"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.ExternalLinkCount"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.HyperLinkCount"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.Id"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.NodeName"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.RelatedLinkCount"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.Rev"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.TeamProject"].Name, false);
                    m_skipFields.Add(VstsConn.store.FieldDefinitions["System.RevisedDate"].Name, false);
                }

                return m_skipFields;
            }
        }

        internal static VSTSConnection VstsConn;

        private static char[] m_unsupportedCSSChars;
        internal static char[] UnsupportedCSSChars
        {
            get 
            {
                if (m_unsupportedCSSChars == null)
                {
                    m_unsupportedCSSChars = new char[] { '/', '$', '?', '&', '*', '"', '<', '>', '|', '#', '%', ':', '\t'};
                }
                return m_unsupportedCSSChars;
            }
        }

        internal const string AreaRoot = "ProjectModelHierarchy";
        internal const string IterationRoot = "ProjectLifeCycle";
    } // end of class VSTSConstants
}
