// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using Microsoft.TeamFoundation.Migration.BusinessModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Public interface to define various Analysis Services that detect conflicts during migration process.
    /// Default implementations exists for VCBasicAnalysisService and WITBasicAnalysisService.
    /// </summary>
    internal interface IConflictAnalysisService
    {
        /// <summary>
        /// ChangeGorupService in case of conflict during migration, conflicts are resolved based on ChangeGroups
        /// </summary>
        ChangeGroupService TargetChangeGroupService
        {
            get;
            set;
        }
        /// <summary>
        /// Conflict Manager to resolve conflicts
        /// </summary>
        ConflictManager ConflictManager
        {
            get;
            set;
        }
        /// <summary>
        /// Translation service to translate source information into tool specific information
        /// </summary>
        ITranslationService TranslationService
        {
            get;
            set;
        }
        /// <summary>
        /// GUID of the target system
        /// </summary>
        Guid TargetSystemId
        {
            get;
            set;
        }
        /// <summary>
        /// GUID of the source system
        /// </summary>
        Guid SourceSystemId
        {
            get;
            set;
        }
        /// <summary>
        /// Holds configuration information
        /// </summary>
        Session Configuration
        {
            get;
            set;
        }
        /// <summary>
        /// Analysis Service calls analyze for processing each ChangeGroup 
        /// </summary>
        void Analyze();
    }
}
