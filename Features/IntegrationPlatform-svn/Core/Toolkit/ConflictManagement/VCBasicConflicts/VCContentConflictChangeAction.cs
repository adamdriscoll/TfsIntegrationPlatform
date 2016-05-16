// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class VCContentConflictTakeLocalChangeAction : ResolutionAction
    {
        static VCContentConflictTakeLocalChangeAction()
        {
            m_VCContentConflictTakeLocalChangeActionRefName = new Guid("D13FC241-B0CD-45fe-B0A5-99C830DBCCBC");
            m_VCContentConflictTakeLocalChangeActionDispName = "Resolve VC content Conflict by always taking local changes";
            m_supportedActionDataKeys = new List<string>();
        }

        public override Guid ReferenceName
        {
            get
            {
                return m_VCContentConflictTakeLocalChangeActionRefName;
            }
        }

        public override string FriendlyName
        {
            get
            {
                return m_VCContentConflictTakeLocalChangeActionDispName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return m_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid m_VCContentConflictTakeLocalChangeActionRefName;
        private static readonly string m_VCContentConflictTakeLocalChangeActionDispName;
        private static readonly List<string> m_supportedActionDataKeys;
    }

    public class VCContentConflictTakeOtherChangesAction : ResolutionAction
    {
        static VCContentConflictTakeOtherChangesAction()
        {
            m_VCContentConflictTakeOtherChangeActionRefName = new Guid("38B0C262-D36E-459d-9502-6E6051DECAFE");
            m_VCContentConflictTakeOtherChangeActionDispName = "Resolve VC content Conflict by always taking other side's changes";
            m_supportedActionDataKeys = new List<string>();
        }

        public override Guid ReferenceName
        {
            get
            {
                return m_VCContentConflictTakeOtherChangeActionRefName;
            }
        }

        public override string FriendlyName
        {
            get
            {
                return m_VCContentConflictTakeOtherChangeActionDispName;
            }
        }

        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get
            {
                return m_supportedActionDataKeys.AsReadOnly();
            }
        }

        private static readonly Guid m_VCContentConflictTakeOtherChangeActionRefName;
        private static readonly string m_VCContentConflictTakeOtherChangeActionDispName;
        private static readonly List<string> m_supportedActionDataKeys;
    }

    public class VCContentConflictUserMergeChangeAction : ResolutionAction
    {
        public static readonly string MigrationInstructionChangeId = "Synced change group name at migration side";
        public static readonly string DeltaTableChangeId = "Synced change group name at delta table side";

        private static readonly Guid m_VCContentConflictUserMergeChangeActionRefname;
        private static readonly string m_VCContentConflictUserMergeChangeActionDispName;
        private static readonly List<string> m_supportedActionDataKeys;


        static VCContentConflictUserMergeChangeAction()
        {
            m_VCContentConflictUserMergeChangeActionRefname = new Guid("131E07C1-1A3F-414a-B592-7AE92667A805");
            m_VCContentConflictUserMergeChangeActionDispName = "Resolve VC content Conflict by allowing the user to merge changes manually";
            m_supportedActionDataKeys = new List<string>();
            m_supportedActionDataKeys.Add(MigrationInstructionChangeId);
            m_supportedActionDataKeys.Add(DeltaTableChangeId);
        }

        public override Guid ReferenceName
        {
            get { return m_VCContentConflictUserMergeChangeActionRefname; }
        }

        public override string FriendlyName
        {
            get { return m_VCContentConflictUserMergeChangeActionDispName; }
        }
        public override ReadOnlyCollection<string> ActionDataKeys
        {
            get { return m_supportedActionDataKeys.AsReadOnly(); }
        }
    }
}
