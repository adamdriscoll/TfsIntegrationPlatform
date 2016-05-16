// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Net;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion.ObjectModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using System.IO;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter.SubversionOM
{
    public class Repository : IDisposable
    {
        #region Private Members

        private Uri m_uri;
        private Uri m_repositoryRoot;

        private NetworkCredential m_credential;
        
        private SubversionClient m_client;

        #endregion

        #region Private Static Members

        private static Dictionary<Uri, Repository> s_repositories = new Dictionary<Uri, Repository>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates and initializes a new connection to the repository
        /// </summary>
        /// <param name="uri">The URI of the repository</param>
        private Repository(Uri uri)
        {
            if (null == uri)
                throw new ArgumentNullException("uri");
            
            m_uri = PathUtils.GetNormalizedPath(uri.AbsoluteUri);
            m_credential = null;
        }

        /// <summary>
        /// Creates and initializes a new connection to the repository
        /// </summary>
        /// <param name="uri">The URI of the repository</param>
        /// <param name="userName">The user to connect to the repository</param>
        /// <param name="password">The password to connect to the repository</param>
        private Repository(Uri uri, string userName, string password) : this (uri)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                m_credential = new NetworkCredential(userName, password);
            }
            else
            {
                m_credential = null;
            }
        }

        #endregion

        #region Factory Method

        /// <summary>
        /// Factory method to create a new instance of an subversion repository object
        /// </summary>
        /// <param name="uri">The URI of the repository</param>
        public static Repository GetRepository(Uri uri)
        {
            if (s_repositories.ContainsKey(uri))
            {
                return s_repositories[uri];
            }

            var repo = new Repository(uri);
            s_repositories.Add(uri, repo);

            return repo;
        }

        /// <summary>
        /// Factory method to create a new instance of repository object
        /// </summary>
        /// <param name="uri">The URI of the repository</param>
        /// <param name="userName">The user to connect to the repository</param>
        /// <param name="password">The password to connect to the repository</param>
        public static Repository GetRepository(Uri uri, string userName, string password)
        {
            if (s_repositories.ContainsKey(uri))
            {
                return s_repositories[uri];
            }

            var repo = new Repository(uri, userName, password);

            // User RepositoryRoot to verify the connection to SVN repository.
            if (repo.RepositoryRoot != null)
            {
                s_repositories.Add(uri, repo);
                return repo;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region public Properties

        /// <summary>
        /// The URI of the repository
        /// </summary>
        public Uri URI
        {
            get
            {
                return m_uri;
            }
        }

        /// <summary>
        /// Gets the actual root folder of the repository
        /// </summary>
        public Uri RepositoryRoot
        {
            get
            {
                EnsureAuthenticated();
                return m_client.VirtualRepositoryRoot;
            }
        }

        #endregion

        #region public Methods

        /// <summary>
        /// Downloads a file from the SVN repository
        /// </summary>
        /// <param name="localPath">The local file path where the file has to be stored</param>
        /// <param name="svnUriTarget">The fully qualified path to the file in the repository</param>
        /// <param name="revision">The revision that has to be downloaded</param>
        public void DownloadFile(string localPath, Uri svnUriTarget, int revision)
        {
            //ensure that the destination directory already exists
            var file = new FileInfo(localPath);
            if(!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            //ensure that the file is not readonly
            if (file.Exists && file.IsReadOnly)
            {
                file.IsReadOnly = false;
            }
            m_client.DownloadItem(svnUriTarget, revision, localPath);
        }

        /// <summary>
        /// Queries the currently latest revision number of the repository
        /// </summary>
        /// <returns></returns>
        public int GetLatestRevisionNumber()
        {
            EnsureAuthenticated();
            return m_client.GetLatestRevisionNumber(m_uri);
        }

        public Dictionary<int, ChangeSet> QueryHistory(Uri path, int startRevision, int limit, bool includeChanges)
        {
            EnsureAuthenticated();
            return m_client.QueryHistory(path, startRevision, limit, includeChanges);
        }

        public Dictionary<int, ChangeSet> QueryHistoryRange(Uri path, int startRevision, int endRevision, bool includeChanges)
        {
            EnsureAuthenticated();
            return m_client.QueryHistoryRange(path, startRevision, endRevision, includeChanges);
        }

        /*/// <summary>
        /// Queries a range <see cref="LogRecord"/> objects from subversion. 
        /// </summary>
        /// <param name="startingRevision">The start revision of the range</param>
        /// <param name="latestRevision">The end revision of the range</param>
        /// <returns>The collection of <see cref="LogRecord"/> objects for the defined range</returns>
        internal IEnumerable<LogRecord> GetLogRecords(long startingRevision, long latestRevision)
        {
            var records = GetLogRecordsInternal(startingRevision, latestRevision);
            foreach (var record in records)
            { 
                yield return new LogRecord(record, this, true);
            }
        }

        /// <summary>
        /// Queries a range <see cref="SvnLogEventArgs"/> objects from subversion. 
        /// This is for internal purpose and should not be used in any other code beside of the OM
        /// </summary>
        /// <param name="startingRevision">The start revision of the range</param>
        /// <param name="latestRevision">The end revision of the range</param>
        /// <returns>The collection of <see cref="SvnLogEventArgs"/> objects for the defined range</returns>
        public IEnumerable<SvnLogEventArgs> GetLogRecordsInternal(long startingRevision, long latestRevision)
        {
            EnsureAuthenticated();

            //configure the logargs. We want to have all the details
            var logargs = new SvnLogArgs();
            logargs.Start = new SvnRevision(startingRevision);
            logargs.End = new SvnRevision(latestRevision);
            logargs.RetrieveChangedPaths = true;

            //Execute the actual query
            Collection<SvnLogEventArgs> logItems;
            if (!m_client.GetLog(URI, logargs, out logItems))
            {
                throw logargs.LastException;
            }

            return logItems;
        }
        */

        
        /*/// <summary>
        /// Queries the svn log for a defined range
        /// </summary>
        /// <param name="path">The repository path that is queried for the history log</param>
        /// <param name="startingRevision">The start revision of the range</param>
        /// <param name="latestRevision">The end revision of the range</param>
        /// <param name="includeChanges">Determines whether the actual changes should be queried as well</param>
        /// <param name="limit">Limits the amount of values to return; 0 is unlimited</param>
        /// <returns>Returns a collection with all LogRecords that match</returns>
        internal IEnumerable<ChangeSet> QueryLog(Uri path, long startingRevision, long latestRevision, int limit , bool includeChanges)
        {
            EnsureAuthenticated();

            //TODO verify wether these paths exists. Otherwiese we cannot query them
            if (!m_client.QueryHistory(path, startingRevision, latestRevision, includeChanges))
            {
                throw logargs.LastException;
            }

            foreach (var logItem in logItems)
            {
                yield return new LogRecord(logItem, this, includeChanges);
            }

            yield break;
        }*/

        /// <summary>
        /// Retrieves the repository guid
        /// </summary>
        /// <returns>The repository guid</returns>
        public Guid GetUniqueId()
        {
            EnsureAuthenticated();
            return m_client.RepositoryId;
        }

        public void EnsureAuthenticated()
        {
            if (null == m_client)
            {
                //Create a new instance of the svn client
                m_client = new SubversionClient();
                m_client.Connect(m_uri, m_credential);
            }
        }

        /// <summary>
        /// Recursivly traverses all the subfiles and folders of a given base directory and returns a collection that contains all records
        /// </summary>
        /// <param name="uri">The base uri to the directory to list all the subfiles and subfolders</param>
        /// <param name="revision">The revision that has to be traversed</param>
        /// <returns>A collection with all subfiles and folders</returns>
        public List<Item> GetItems(Uri uri, int revision, Depth depth)
        {
            //Remark: This method may return thousands of records. Therefore it might has to limit the amount for once pass. 

            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            EnsureAuthenticated();

            return m_client.GetItems(uri, revision, depth);
 
        }

        /// <summary>
        /// Recursivly traverses all the subfiles and folders of a given base directory and returns a collection that contains all records
        /// </summary>
        /// <param name="uri">The base uri to the directory to list all the subfiles and subfolders</param>
        /// <param name="revision">The revision that has to be traversed</param>
        /// <returns>A collection with all subfiles and folders</returns>
        public List<Item> GetItems(string path, int revision, bool recurse)
        {
            //Remark: This method may return thousands of records. Therefore it might has to limit the amount for once pass. 

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            EnsureAuthenticated();

            if(recurse)
            {
                return m_client.GetItems(new Uri(path), revision, Depth.Infinity);
            }
            else
            {
                return m_client.GetItems(new Uri(path), revision, Depth.Immediates);
            }
        }

        /// <summary>
        /// Retrieves a summary of the differences between two files or folders at a given revision
        /// </summary>
        /// <param name="uri1">The reference uri for calculating the differences</param>
        /// <param name="revision1">The revision of the base uri</param>
        /// <param name="uri2">The uri which is compared to the base or reference uri</param>
        /// <param name="revision2">The revision of the second uri</param>
        /// <returns></returns>
        internal bool GetDiffSumary(string path1, int revision1, string path2, int revision2)
        {
            EnsureAuthenticated();
            return m_client.HasContentChange(new Uri(path1), revision1, new Uri(path2), revision2);           
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (null != m_client)
            {
                m_client.Dispose();
                m_client = null;
            }
        }

        #endregion
    }
}
