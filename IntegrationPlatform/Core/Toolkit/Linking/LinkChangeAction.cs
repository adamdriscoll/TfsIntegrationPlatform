// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Linking
{
    public class LinkChangeAction
    {
        public enum LinkChangeActionStatus
        {
            Undefined = 0,
            Created = 10,
            SkipScopedOutVCLinks = 11,
            DeltaCompleted = 15,
            Translated = 20,
            SkipScopedOutWILinks = 21,
            ReadyForMigration = 30,
            Completed = 40,
            Skipped = 50,
        }

        public LinkChangeAction(
            Guid changeActionId,
            ILink link,
            LinkChangeActionStatus status,
            bool isConflicted,
            int executionOrder,
            LinkChangeGroup group)
        {
            Initialize(changeActionId, link, status, isConflicted, executionOrder, group, INVALID_INTERNAL_ID);
        }

        public LinkChangeAction(
            Guid changeActionId,
            ILink link,
            LinkChangeActionStatus status,
            bool isConflicted)
        {
            Initialize(changeActionId, link, status, isConflicted, 0, null, INVALID_INTERNAL_ID);
        }

        internal LinkChangeAction(
            Guid changeActionId,
            ILink link,
            LinkChangeActionStatus status,
            bool isConflicted,
            int executionOrder,
            LinkChangeGroup group,
            long internalId)
        {
            if (internalId <= 0)
            {
                throw new ArgumentOutOfRangeException("internalId", "internalId must be a positive integer");
            }
            Initialize(changeActionId, link, status, isConflicted, executionOrder, group, internalId);
        }

        internal LinkChangeAction(
            Guid changeActionId,
            ILink link,
            LinkChangeActionStatus status,
            bool isConflicted,
            long internalId)
        {
            if (internalId <= 0)
            {
                throw new ArgumentOutOfRangeException("internalId", "internalId must be a positive integer");
            }
            Initialize(changeActionId, link, status, isConflicted, 0, null, internalId);
        }

        internal static int GetStatusStorageValue(LinkChangeActionStatus status)
        {
            return (int)status;
        }

        private void Initialize(
            Guid changeActionId,
            ILink link,
            LinkChangeActionStatus status,
            bool isConflicted,
            int executionOrder,
            LinkChangeGroup group,
            long internalId)
        {
            if (null == link)
            {
                throw new ArgumentNullException("link");
            }

            ChangeActionId = changeActionId;
            Link = link;
            Status = status;
            IsConflicted = isConflicted;
            InternalId = internalId;
            ExecutionOrder = executionOrder;
            Group = group;
        }

        public Guid ChangeActionId
        {
            get;
            internal set;
        }

        public ILink Link
        {
            get;
            private set;
        }

        public LinkChangeActionStatus Status
        {
            get;
            set;
        }

        public bool IsConflicted
        {
            get;
            set;
        }

        public LinkChangeGroup Group
        {
            get;
            internal set;
        }

        public int ExecutionOrder
        {
            get;
            set;
        }

        /// <summary>
        /// ServerLinkChangeId can optionally be set by a LinkProvider when creating a LinkChangeAction if the Server that it is supporting
        /// provides a unique identifier for every link change made on the server
        /// In the bidirectional sync case, this allows the platform to ensure that link changes made by the sync in one direction are not
        /// considered user link changes that need to be sync'd back in the other direction.
        /// </summary>
        public string ServerLinkChangeId
        {
            get;
            set;
        }

        internal long InternalId
        {
            get;
            set;
        }

        public const long INVALID_INTERNAL_ID = long.MinValue;
    }
}