// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2008VCAdapter
{
    public class TfsVCSyncMonitorProvider : ISyncMonitorProvider
    {
        private VersionControlServer m_tfsClient;
        private ITranslationService m_translationService;
        private MigrationSource m_migrationSource;
        private string m_skipComment;

        /// <summary>
        /// Obtain references to services needed by this class
        /// </summary>
        public void InitializeServices(IServiceContainer syncMonitorServiceContainer)
        {
            m_translationService = syncMonitorServiceContainer.GetService(typeof(ITranslationService)) as ITranslationService;
            Debug.Assert(m_translationService != null);

            ConfigurationService configService = syncMonitorServiceContainer.GetService(typeof(ConfigurationService)) as ConfigurationService;
            m_skipComment = configService == null ? "**NOMIGRATION**" : configService.GetValue<string>(Constants.SkipComment, "**NOMIGRATION**");
        }

        /// <summary>
        /// Perform the adapter-specific initialization
        /// </summary>
        public virtual void InitializeClient(MigrationSource migrationSource)
        {
            m_migrationSource = migrationSource;
            TeamFoundationServer tfsServer = TeamFoundationServerFactory.GetServer(migrationSource.ServerUrl);
            m_tfsClient = (VersionControlServer)tfsServer.GetService(typeof(VersionControlServer));
            m_tfsClient.NonFatalError += new ExceptionEventHandler(NonFatalError);
        }

        private void NonFatalError(object sender, ExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                processNonFatalErrorException(e.Exception);
            }

            if (e.Failure != null)
            {
                Trace.TraceWarning(e.Failure.Message);
            }
        }

        /// <summary>
        /// Process the exceptions in returned from nonfatalerror
        /// </summary>
        /// <param name="exception"></param>
        private void processNonFatalErrorException(Exception exception)
        {
            // For now we always throw the exception, but leaving this method here in case that changes
            throw exception;
        }

        #region ISyncMonitorProvider implementation
        public ChangeSummary GetSummaryOfChangesSince(string lastProcessedChangeItemId, List<string> filterStrings)
        {
            ChangeSummary changeSummary = new ChangeSummary();
            changeSummary.ChangeCount = 0;
            changeSummary.FirstChangeModifiedTimeUtc = DateTime.MinValue;

            int lastProcessedChangesetId = 0;
            Changeset lastProcessedChangeset;

            try
            {
                lastProcessedChangesetId = int.Parse(lastProcessedChangeItemId);
            }
            catch(FormatException)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture,
                    TfsVCAdapterResource.InvalidChangeItemIdFormat, lastProcessedChangeItemId));
            }
            lastProcessedChangeset = m_tfsClient.GetChangeset(lastProcessedChangesetId, false, false);
            changeSummary.FirstChangeModifiedTimeUtc = lastProcessedChangeset.CreationDate.ToUniversalTime();

            Changeset nextChangesetToBeMigrated = null;

            if (m_tfsClient.GetLatestChangesetId() > lastProcessedChangesetId)
            {
                // Query history for each filter string path
                foreach (string path in filterStrings)
                {
                    VersionSpec versionSpecFrom = new ChangesetVersionSpec(lastProcessedChangesetId + 1);

                    IEnumerable changesets = m_tfsClient.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full, null, versionSpecFrom, VersionSpec.Latest, Int32.MaxValue, false, true);

                    try
                    {
                        foreach (Changeset cs in changesets)
                        {
                            if (!m_translationService.IsSyncGeneratedItemVersion(
                                    cs.ChangesetId.ToString(),
                                    Constants.ChangeGroupGenericVersionNumber,
                                    new Guid(m_migrationSource.InternalUniqueId)) &&
                                !cs.Comment.Contains(m_skipComment))
                            {
                                changeSummary.ChangeCount++;

                                if (nextChangesetToBeMigrated == null || cs.ChangesetId < nextChangesetToBeMigrated.ChangesetId)
                                {
                                    nextChangesetToBeMigrated = cs;
                                }
                            }
                        }
                    }
                    catch (ItemNotFoundException)
                    {
                        // swallow if there are no changesets
                    }
                }
            }

            if (nextChangesetToBeMigrated != null)
            {
                changeSummary.FirstChangeModifiedTimeUtc = nextChangesetToBeMigrated.CreationDate.ToUniversalTime();
            }

            return changeSummary;
        }

        #endregion

        #region IServiceProvider implementation
        /// <summary>
        /// Gets the service object of the specified type. 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Object GetService(Type serviceType)
        {
            return this as IServiceProvider;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        }
        #endregion
    }

}
