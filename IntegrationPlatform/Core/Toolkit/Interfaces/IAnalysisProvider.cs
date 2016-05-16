// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The common analysis interface that all provider needs to implement.
    /// </summary>
    public interface IAnalysisProvider : IServiceProvider, IDisposable
    {
        /// <summary>
        /// List of change actions supported by the analysis provider. 
        /// </summary>
        Dictionary<Guid, ChangeActionHandler> SupportedChangeActions
        {
            get;
        }

        /// <summary>
        /// List of change actions supported by the other side. 
        /// </summary>
        ICollection<Guid> SupportedChangeActionsOther
        {
            set;
        }

        /// <summary>
        /// List of content types supported by this provider
        /// </summary>
        Collection<ContentType> SupportedContentTypes
        {
            get;
        }

        /// <summary>
        /// List of content types supported by the other side
        /// </summary>
        Collection<ContentType> SupportedContentTypesOther
        {
            set;
        }

        /// <summary>
        /// Initialize method of the analysis provider - acquire references to the services provided by the platform.
        /// </summary>
        void InitializeServices(IServiceContainer analysisServiceContainer);

        /// <summary>
        /// Initialize method of the analysis provider.
        /// Please implement all the heavey-weight initialization logic here, e.g. server connection.
        /// </summary>
        void InitializeClient();

        /// <summary>
        /// Register adapter's supported change actions.
        /// </summary>
        void RegisterSupportedChangeActions(ChangeActionRegistrationService changeActionRegistrationService);

        /// <summary>
        /// Register adapter's supported content types.
        /// </summary>
        void RegisterSupportedContentTypes(ContentTypeRegistrationService contentTypeRegistrationService);

        /// <summary>
        /// Register adapter's conflict handlers.
        /// </summary>
        void RegisterConflictTypes(ConflictManager conflictManager);

        /// <summary>
        /// Generate the context info table
        /// </summary>
        void GenerateContextInfoTable();

        /// <summary>
        /// Generate the delta table.
        /// </summary>
        void GenerateDeltaTable();

        /// <summary>
        /// Detects adapter-specific conflicts.
        /// </summary>
        /// <param name="changeGroup"></param>
        void DetectConflicts(ChangeGroup changeGroup);

        /// <summary>
        /// Gets a unique string to identify the endpoint system, from which the migration data is retrieved from and written to
        /// </summary>
        /// <param name="migrationSourceConfig"></param>
        /// <returns></returns>
        string GetNativeId(Microsoft.TeamFoundation.Migration.BusinessModel.MigrationSource migrationSourceConfig);
    }
}
