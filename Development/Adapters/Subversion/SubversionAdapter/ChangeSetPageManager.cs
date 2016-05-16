// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion.ObjectModel;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.SubversionOM;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    internal class ChangeSetPageManager
    {
        #region Private Members

        private Repository m_repository;
        
        //Array that contains all the revisions and a index that points to the current element
        private int[] m_revisions;
        private int m_currentIndex;

        //describes how many records should be queried at a time
        private int m_pageSize;

        //the lowest and the highest revision that is in the current page
        private int m_pageStartRevision;
        private int m_pageEndRevision;

        private Dictionary<int, ChangeSet> m_cache;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the cache manager
        /// </summary>
        /// <param name="repository">The repository that is used to query the change details</param>
        /// <param name="revisions">All the revisions numbers of the changes that will be queried</param>
        /// <param name="pageSize">The page size of the cache</param>
        internal ChangeSetPageManager(Repository repository, int[] revisions, int pageSize)
        {
            if (null == repository)
            {
                throw new ArgumentNullException("repository");
            }

            if (null == revisions)
            {
                throw new ArgumentNullException("revisions");
            }

            m_repository = repository;
            m_revisions = revisions;

            if (pageSize <= 0)
            {
                pageSize = 50;
            }

            m_pageSize = pageSize;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the revision number of the latest commited change
        /// </summary>
        internal int HeadRevision
        {
            get
            {
                if (m_revisions.Length > 0)
                {
                    return m_revisions[m_revisions.Length - 1];
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Gets the revision number of the currently selected element
        /// </summary>
        internal int CurrentRevision
        {
            get
            {
                if (m_revisions.Length > 0)
                {
                    return m_revisions[CurrentIndex];
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Gets the currently selected <see cref="LogRecord"/> object
        /// </summary>
        internal ChangeSet Current
        {
            get
            {
                if (CurrentRevision > 0)
                {
                    if (null == m_cache)
                    {
                        LoadPage();
                    }

                    if (m_cache.ContainsKey(CurrentRevision))
                    {
                        return m_cache[CurrentRevision];
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the index that points to the current element
        /// </summary>
        internal int CurrentIndex
        {
            get
            {
                return m_currentIndex;
            }
            private set
            {
                m_currentIndex = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Progresses to the next element in the collection
        /// </summary>
        /// <returns>True if the element is valid; false if we reached the end of the collection</returns>
        internal bool MoveNext()
        {
            //We only move forward. Therefore we can drop the current record, if it is still in the cache
            if (m_cache.ContainsKey(CurrentRevision))
            {
                m_cache.Remove(CurrentRevision);
            }

            //now we can progress to the next element
            CurrentIndex++;

            //Test wether the next element is still in bound. If not, we reached the end and we have to quit
            if (CurrentIndex >= m_revisions.Length)
            {
                return false;
            }

            //check wether the next revision number is still in this page. If not, we have to load the next one
            if (CurrentRevision > m_pageEndRevision)
            {
                LoadPage();
            }

            return true;
        }

        internal void Reset()
        {
            CurrentIndex = 0;

            m_pageStartRevision = 0;
            m_pageEndRevision = 0;

            m_cache = null;
        }

        #endregion

        #region Private Methods

        private void LoadPage()
        {
            if (0 == m_revisions.Length)
            {
                m_pageStartRevision = 0;
                m_pageEndRevision = 0;

                m_cache = new Dictionary<int, ChangeSet>();
            }
            else
            {
                m_pageStartRevision = m_revisions[CurrentIndex];
                m_pageEndRevision = Math.Min(HeadRevision, m_pageStartRevision + m_pageSize);

                //Query the log records and store it in a lookup table for fast access
                // Todo, should use mapping path here
                m_cache = m_repository.QueryHistoryRange(m_repository.RepositoryRoot, m_pageStartRevision, m_pageEndRevision, true);
            }
        }

        #endregion
    }
}
