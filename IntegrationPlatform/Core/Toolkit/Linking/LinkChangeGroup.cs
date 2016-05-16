// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class LinkChangeGroup
    {
        public enum LinkChangeGroupStatus
        {
            Unknown = 0,
            Created = 10,
            InAnalysisDeferred = 15,
            InAnalysis = 20,
            InAnalysisTranslated = 25,
            ReadyForMigration = 30,
            Completed = 40,
        }

        public LinkChangeGroup(string groupName, LinkChangeGroupStatus status, bool isConflicted)
        {
            Initialize(groupName, status, isConflicted, false, INVALID_INTERNAL_ID, 0, 0);
        }

        public LinkChangeGroup(string groupName, LinkChangeGroupStatus status, bool isConflicted, bool isForcedSync)
        {
            Initialize(groupName, status, isConflicted, isForcedSync, INVALID_INTERNAL_ID, 0, 0);
        }

        internal LinkChangeGroup(string groupName, LinkChangeGroupStatus status, bool isConflicted, long internalId, int age, int numOfTranslationRetries)
        {
            if (internalId <= 0)
            {
                throw new ArgumentOutOfRangeException("internalId", "internalId must be a positive integer");
            }
            Initialize(groupName, status, isConflicted, false, internalId, age, numOfTranslationRetries);
        }

        private void Initialize(string groupName, LinkChangeGroupStatus status, bool isConflicted, bool isForcedSync, long internalId, int age, int numOfTranslationRetries)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentNullException("groupName");
            }

            m_actions = new List<LinkChangeAction>();
            Status = status;
            IsConflicted = isConflicted;
            IsForcedSync = isForcedSync;
            InternalId = internalId;
            GroupName = groupName;
            Age = age;
            TranslationRetries = numOfTranslationRetries;
        }

        public void AddChangeAction(LinkChangeAction action)
        {
            if (action == null)
            {
                return;
            }

            if (m_actions.Contains(action))
            {
                return;
            }

            action.Group = this;
            m_actions.Add(action);
        }

        public void DeleteChangeAction(LinkChangeAction action)
        {
            if (m_actions.Contains(action))
            {
                m_actions.Remove(action);
            }
        }

        public List<LinkChangeAction> Actions
        {
            get
            {
                return m_actions;
            }
        }

        public LinkChangeGroupStatus Status
        {
            get;
            set;
        }

        public bool IsConflicted
        {
            get;
            set;
        }

        public bool IsForcedSync
        {
            get;
            set;
        }

        public string GroupName
        {
            get; 
            internal set;
        }

        internal long InternalId
        {
            get;
            set;
        }

        internal int Age
        {
            get;
            set;
        }

        internal int TranslationRetries
        {
            get;
            set;
        }

        internal void PrependActions(List<LinkChangeAction> actions)
        {
            if (null != actions && actions.Count > 0)
            {
                foreach (var action in actions)
                {
                    m_actions.Insert(0, action);
                }
            }
        }

        public const long INVALID_INTERNAL_ID = long.MinValue;
        List<LinkChangeAction> m_actions;
    }
}