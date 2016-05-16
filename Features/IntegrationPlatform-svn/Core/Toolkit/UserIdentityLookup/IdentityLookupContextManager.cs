// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    class IdentityLookupContextManager
    {
        Dictionary<Guid, Guid> m_leftToRightMigrationSourcePairs = new Dictionary<Guid, Guid>();

        public IdentityLookupContextManager(
            NotifyingCollection<Session> sessions)
        {
            foreach (Session s in sessions)
            {
                Guid leftMigrSrcId = new Guid(s.LeftMigrationSourceUniqueId);
                Guid rightMigrSrcId = new Guid(s.RightMigrationSourceUniqueId);
                Debug.Assert(!m_leftToRightMigrationSourcePairs.ContainsKey(leftMigrSrcId), "Duplicate left Migration Source Ids");

                m_leftToRightMigrationSourcePairs.Add(leftMigrSrcId, rightMigrSrcId);
            }
        }

        /// <summary>
        /// This method tries to look up in the sessions to find the source Migration 
        /// Source that context.SourceMigrationSourceId corresponds to. Then it verifies
        /// that context.TargetMigrationSourceId corresponds to the peer Migration Source
        /// in the same session. 
        /// If the mapping session found, the direction of mapping (context.MappingDirection)
        /// is set accordingly. 
        /// Note that only LeftToRight and RightToLeft direction enum is used to set the
        /// MappingDirection
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool TrySetupContext(IdentityLookupContext context)
        {
            if (m_leftToRightMigrationSourcePairs.ContainsKey(context.SourceMigrationSourceId))
            {
                // source migration source id is "left"

                if (context.TargetMigrationSourceId.Equals(m_leftToRightMigrationSourcePairs[context.SourceMigrationSourceId]))
                {
                    // bingo: target migration source id is "right"

                    context.MappingDirection = MappingDirectionEnum.LeftToRight;
                    return true;
                }
            }
            else if (m_leftToRightMigrationSourcePairs.ContainsKey(context.TargetMigrationSourceId))
            {
                // target migration source id is "right"

                if (context.SourceMigrationSourceId.Equals(m_leftToRightMigrationSourcePairs[context.TargetMigrationSourceId]))
                {
                    // bingo: source migration source id is "right"

                    context.MappingDirection = MappingDirectionEnum.RightToLeft;
                    return true;
                }
            }

            return false;
        }
    }
}
