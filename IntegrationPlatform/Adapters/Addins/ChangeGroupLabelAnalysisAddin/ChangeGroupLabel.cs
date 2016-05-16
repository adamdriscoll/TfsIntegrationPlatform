// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace ChangeGroupLabelAnalysisAddin
{
    public class ChangeGroupLabel : ILabel
    {
        private ChangeGroup m_changeGroup;
        private string m_name;
        private string m_commment;
        private Dictionary<string, ILabelItem> m_labelItems = new Dictionary<string, ILabelItem>(StringComparer.OrdinalIgnoreCase);
        private ConfigurationService m_configurationService;
        private IServerPathTranslationService m_serverPathTranslationService;

        public ChangeGroupLabel(ChangeGroup changeGroup, AnalysisContext analysisContext)
        {
            m_changeGroup = changeGroup;

            m_configurationService = analysisContext.TookitServiceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;
            if (m_configurationService == null)
            {
                throw new ArgumentNullException("ConfigurationService");
            }

            m_serverPathTranslationService = analysisContext.TookitServiceContainer.GetService(typeof(IServerPathTranslationService)) as IServerPathTranslationService;
            if (m_serverPathTranslationService == null)
            {
                throw new ArgumentNullException("IServerPathTranslationService");
            }

            HashSet<string> deletedItems = new HashSet<string>();

            foreach (IMigrationAction action in changeGroup.Actions)
            {
                if (action.Action == WellKnownChangeActionId.Add ||
                    action.Action == WellKnownChangeActionId.Branch ||
                    action.Action == WellKnownChangeActionId.Edit ||
                    action.Action == WellKnownChangeActionId.Merge ||
                    action.Action == WellKnownChangeActionId.Rename ||
                    action.Action == WellKnownChangeActionId.Undelete)
                {
                    if (!m_labelItems.ContainsKey(action.Path))
                    {
                        m_labelItems.Add(action.Path, new ChangeGroupLabelItem(action));
                    }
                }

                // Also add label items for implicit changes to the parent folder for any items that were added, deleted, or renamed.
                // This occurs with ClearCase (and perhaps other servers)
                // TODO: Add CustomSetting to allow disabling this?
                if (action.Action == WellKnownChangeActionId.Add ||
                    action.Action == WellKnownChangeActionId.Delete ||
                    action.Action == WellKnownChangeActionId.Rename ||
                    action.Action == WellKnownChangeActionId.Undelete)
                {
                    // Note: this depends on the fact that all ChangeAction Paths are in canonical form ('/' for separator)
                    int lastSeparator = action.Path.LastIndexOf('/');
                    string parentPath = (lastSeparator == -1) ? @"/" : action.Path.Substring(0, lastSeparator);
                    // Don't add more than one label item for the same parent folder
                    if (!m_labelItems.ContainsKey(parentPath) && IsPathMapped(parentPath, analysisContext))
                    {
                        m_labelItems.Add(parentPath, new ChangeGroupLabelItem(parentPath));
                    }
                }

                if (action.Action == WellKnownChangeActionId.Delete)
                {
                    if (!deletedItems.Contains(action.Path))
                    {
                        deletedItems.Add(action.Path);
                    }
                }
                if (action.Action == WellKnownChangeActionId.Rename && !string.IsNullOrEmpty(action.FromPath))
                {
                    if (!deletedItems.Contains(action.FromPath))
                    {
                        deletedItems.Add(action.FromPath);
                    }
                }
            }

            // Remove any deleted items from the label items
            foreach (string deletedItem in deletedItems)
            {
                m_labelItems.Remove(deletedItem);
            }

        }

        private bool IsPathMapped(string path, AnalysisContext analysisContext)
        {

            // TraceManager.TraceInformation("IsPathMapped: Entering with path: " + path); 
            string translatedPath = m_serverPathTranslationService.TranslateToCanonicalPathCaseSensitive(path);
            // TraceManager.TraceInformation("IsPathMapped: translatedPath: " + translatedPath); 
            foreach (MappingEntry mapping in m_configurationService.Filters)
            {
                // TraceManager.TraceInformation("IsPathMapped: mapping.Path: " + mapping.Path); 
                string canonicalFilterString = m_serverPathTranslationService.TranslateToCanonicalPathCaseSensitive(mapping.Path);
                // TraceManager.TraceInformation("IsPathMapped: canonicalFilterString: " + canonicalFilterString); 
                if (translatedPath.StartsWith(canonicalFilterString, StringComparison.OrdinalIgnoreCase))
                {
                    // TraceManager.TraceInformation("IsPathMapped: returning true"); 
                    return true;
                }
            }

            // TraceManager.TraceInformation("IsPathMapped: returning false"); 
            return false;
        }

        // Summary:
        //     The comment associated with the label It may be null or empty
        public string Comment
        {
            get
            {
                if (m_commment == null)
                {
                    m_commment = String.Format(CultureInfo.InvariantCulture,
                        ChangeGroupLabelAnalysisAddinResources.LabelCommentFormat, m_changeGroup.Name);
                }
                return m_commment;
            }
            set
            {
                m_commment = value;
            }
        }

        //
        // Summary:
        //     The set of items included in the label
        public List<ILabelItem> LabelItems
        {
            get { return new List<ILabelItem>(m_labelItems.Values); }
        }

        //
        // Summary:
        //     The name of the label (a null or empty value is invalid)
        public string Name
        {
            get
            {
                if (m_name == null)
                {
                    m_name = string.Format(CultureInfo.InvariantCulture, ChangeGroupLabelAnalysisAddinResources.LabelNameFormat, 
                        m_changeGroup.Name, DateTime.Now);
                }
                return m_name;
            }
            set
            {
                m_name = value;
            }
        }

        //
        // Summary:
        //     The name of the owner (it may be null or empty)
        public string OwnerName
        {
            get { return null; }
        }

        //
        // Summary:
        //     The scope is a server path that defines the namespace for labels in some
        //     VC servers In this case, label names must be unique within the scope, but
        //     two or more labels with the same name may exist as long as their Scopes are
        //     distinct.  It may be return string.Empty is source from a VC server that does not
        //     have the notion of label scopes
        public string Scope
        {
            get
            {
                // The migration provider should use the default scope as appropriate on the system it is adapting
                return string.Empty;
            }
        }
    }
}
