// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class PagedCollectionEnum<T> : IEnumerator<T>
    {
        // Enumerators are positioned before the first element until the first MoveNext() call.
        int m_currentPosition = -1;
        public PagedCollection<T> m_items;

        public PagedCollectionEnum(PagedCollection<T> items)
        {
            m_items = items;
        }

        #region IEnumerator<T> Members
        
        public T Current
        {
            get
            {
                try
                {
                    return m_items[m_currentPosition];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
        
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        #endregion

        #region IEnumerator Members

        object IEnumerator.Current
        {
            get { return (object) this.Current; }
        }

        public bool MoveNext()
        {
            m_currentPosition++;
            return (m_currentPosition < m_items.Count);
        }

        public void Reset()
        {
            m_currentPosition = -1;
        }
        #endregion
    }
}
