// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class PagedCollection<T> : IList<T>
    {
        private int m_count = -1;
        private int m_pageSize;
        private int m_pageTimeout; 
        private IItemsProvider<T> m_itemsProvider;

        private readonly Dictionary<int, IList<T>> m_pages = new Dictionary<int, IList<T>>();
        private readonly Dictionary<int, DateTime> m_pageAccessTimes = new Dictionary<int, DateTime>();

        public PagedCollection(IItemsProvider<T> itemsProvider, int pageSize, int pageTimeout)
        {
            m_itemsProvider = itemsProvider;
            m_pageSize = pageSize;
            m_pageTimeout = pageTimeout;
        }

        internal void SaveActivePage()
        {
            foreach (int pageIndex in m_pages.Keys)
            {
                this.StorePage(pageIndex);
            }
        }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            // TODO: Provider knows how to look up items best... add to interface
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            InsertItem(index, item);
        }

        public void RemoveAt(int index)
        {
            // TODO: The code below would work for an in memory collection, but the one instance of RemoveAt in our code looks a little odd
            throw new NotImplementedException();

            //RemoveItem(index);
        }

        public T this[int index]
        {
            get
            {
                return GetItem(index);
            }
            set 
            { 
                throw new NotSupportedException(); 
            }
        }

        public void FlushPages(int activePageIndex)
        {
            List<int> keys = new List<int>(m_pageAccessTimes.Keys);
            foreach (int key in keys)
            {
                // Do not unload the current page regardless of the timestamp
                if ((key != activePageIndex) && (DateTime.Now - m_pageAccessTimes[key]).TotalMilliseconds > PageTimeout)
                {
                    // The provider will verify that the page is not persisted to DB before attempting the store
                    StorePage(key);
                    m_pages.Remove(key);
                    m_pageAccessTimes.Remove(key);
                }
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            // Insert into the last position
            int index = this.Count;
            InsertItem(index, item);
            Count++;
        }

        public void Clear()
        {
            ClearItems();
        }

        public bool Contains(T item)
        {
            int index = this.IndexOf(item);

            if (index < 0)
            {
                return false;
            }

            return true;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // TODO - What sensible thing does a PagedCollection do with a copy request?
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                if (m_count == -1)
                {
                    m_count = ItemsProvider.Count();
                }
                return m_count;
            }
            protected set
            {
                m_count = value;
            }
        }

        public bool IsReadOnly
        {
            get 
            { 
                return false; 
            }
        }

        public bool Remove(T item)
        {
            int index = this.IndexOf(item);

            if (index < 0)
            {
                return false;
            }

            RemoveItem(index);
            return true;
        }
        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return new PagedCollectionEnum<T>(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        protected virtual T GetItem(int index)
        {
            int pageIndex = index / PageSize;
            int pageOffset = index % PageSize;

            RequestPage(pageIndex);

            // Flush old pages, except the one we are processing
            FlushPages(pageIndex);

            return m_pages[pageIndex][pageOffset];
        }

        protected virtual void InsertItem(int index, T item)
        {
            int pageIndex = index / PageSize;
            int pageOffset = index % PageSize;

            // Check for page boundary
            if (pageIndex != 0 && pageOffset == 0)
            {
                StorePage(pageIndex - 1);
            }

            RequestPage(pageIndex);
            m_pages[pageIndex].Insert(pageOffset, item);

            // Flush old pages, except the one we are processing
            FlushPages(pageIndex);
        }

        protected virtual void ClearItems()
        {
            m_pages.Clear();
            m_pageAccessTimes.Clear();
        }

        protected virtual void RemoveItem(int index)
        {
            int pageIndex = index / PageSize;
            int pageOffset = index % PageSize;

            RequestPage(pageIndex);

            // TODO - Seems flaky to remove from memory but not backing store
            //
            // Propagate Remove to provider?
            m_pages[pageIndex].RemoveAt(pageOffset);
        }

        protected virtual void SetItem(int index, T item)
        {
            int pageIndex = index / PageSize;
            int pageOffset = index % PageSize;

            RequestPage(pageIndex);

            // TODO - Seems flaky to change in memory but not backing store
            //
            // Propagate Set to provider?
            m_pages[pageIndex][pageOffset] = item;
        }

        protected virtual void RequestPage(int pageIndex)
        {
            if (! m_pages.ContainsKey(pageIndex))
            {
                m_pages.Add(pageIndex, null);
                m_pageAccessTimes.Add(pageIndex, DateTime.Now);
                LoadPage(pageIndex);
            }
            else
            {
                m_pageAccessTimes[pageIndex] = DateTime.Now;
            }
        }

        protected virtual void LoadPage(int pageIndex)
        {
            if (m_pages.ContainsKey(pageIndex))
            {
                m_pages[pageIndex] = ItemsProvider.LoadPage(pageIndex * PageSize, PageSize);
            }
        }

        protected virtual void StorePage(int pageIndex)
        {
            if (m_pages.ContainsKey(pageIndex))
            {
                ItemsProvider.StorePage(pageIndex * PageSize, m_pages[pageIndex]);
            }
        }

        private IItemsProvider<T> ItemsProvider
        {
            get
            {
                return m_itemsProvider;
            }
        }

        private int PageSize
        {
            get
            {
                return m_pageSize;
            }
        }

        private int PageTimeout
        {
            get
            {
                return m_pageTimeout;
            }
        }
    }
}