﻿// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Microsoft.TeamFoundation.Migration.Toolkit;

namespace SemaphoreFileAnalysisAddin
{
    public class SubTreeLabel : ILabel
    {
        private string m_fileSystemPath;
        private string m_name;
        private string m_commment;
        private string m_targetSideScope;
        private List<ILabelItem> m_labelItems = new List<ILabelItem>();

        public SubTreeLabel(string fileSystemPath, string targetSideScope)
        {
            if (string.IsNullOrEmpty(fileSystemPath))
            {
                throw new ArgumentException("fileSystemPath");
            }
            m_fileSystemPath = fileSystemPath;

            if (string.IsNullOrEmpty(targetSideScope))
            {
                throw new ArgumentException("targetSideScope");
            }
            m_targetSideScope = targetSideScope;
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
                        SemaphoreFileAnalysisAddinResources.LabelCommentFormat, m_fileSystemPath);
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
            get { return m_labelItems; }
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
                    // Generate a label that includes an indication that the label was generated by the TFS Integration platform and the current date time
                    m_name = String.Format(CultureInfo.InvariantCulture, 
                        SemaphoreFileAnalysisAddinResources.DefaultLabelNameFormat,
                        DateTime.Now);
                }
                return m_name;
            }
            set
            {
                m_name = FixupLabelName(value);
            }
        }

        public static string FixupLabelName(string labelName)
        {
            char [] invalidTfsLabelChars = new char [] { '"', '/', ':', '<', '>', '\\', '|', '*', '?', '@' };
            int index;
            do
            {
                index = labelName.IndexOfAny(invalidTfsLabelChars);
                if (index != -1)
                {
                    labelName = labelName.Replace(labelName.Substring(index, 1), "-");
                }
            }
            while (index != -1);

            return labelName;
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
        //     distinct.  It may be null or empty is source from a VC server that does not
        //     have the notion of label scopes
        public string Scope
        {
            get { return m_targetSideScope; }
        }
    }
}