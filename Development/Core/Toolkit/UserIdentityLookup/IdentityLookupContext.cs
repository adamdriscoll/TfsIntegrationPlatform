// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Context information to fullfil the user identity lookup
    /// </summary>
    public sealed class IdentityLookupContext
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sourceMigrationSourceId"></param>
        public IdentityLookupContext(
            Guid sourceMigrationSourceId,
            Guid targetMigrationSourceId)
        {
            SourceMigrationSourceId = sourceMigrationSourceId;
            TargetMigrationSourceId = targetMigrationSourceId;
        }

        /// <summary>
        /// Gets the Unique Id of the Migration Source, to which the original identity belongs
        /// </summary>
        public Guid SourceMigrationSourceId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Unique Id of the Migration Source, to which the translated identity belongs
        /// </summary>
        public Guid TargetMigrationSourceId
        {
            get;
            private set;
        }

        internal MappingDirectionEnum MappingDirection
        {
            get;
            set;
        }

        internal IdentityLookupContext Reverse()
        {
            IdentityLookupContext retVal = new IdentityLookupContext(TargetMigrationSourceId, SourceMigrationSourceId);
            retVal.MappingDirection = (this.MappingDirection == MappingDirectionEnum.LeftToRight 
                                      ? MappingDirectionEnum.RightToLeft 
                                      : MappingDirectionEnum.LeftToRight);
            return retVal;
        }
    }
}
