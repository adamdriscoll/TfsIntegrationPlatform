// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Symmetric diff class.
    /// </summary>
    /// <typeparam name="T">Member type</typeparam>
    public class SymDiff<T> : IDisposable
    {
        private List<T> m_leftOnly;                         // Items existing only on the left side
        private List<T> m_rightOnly;                        // Items existing only on the right side
        private bool m_disposed;

        /// <summary>
        /// Returns items existing only on the left side.
        /// </summary>
        public List<T> LeftOnly { get { return m_leftOnly; } }

        /// <summary>
        /// Returns items existing only on the right side.
        /// </summary>
        public List<T> RightOnly { get { return m_rightOnly; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="left">Items on the left</param>
        /// <param name="right">Items on the right</param>
        /// <param name="cmp">Comparer</param>
        public SymDiff(
            T[] left,
            T[] right,
            IComparer<T> cmp)
        {
            m_disposed = false;

            int l, r;
            m_leftOnly = new List<T>();
            m_rightOnly = new List<T>();

            for (l = 0, r = 0; l < left.Length && r < right.Length; )
            {
                int n = cmp.Compare(left[l], right[r]);

                if (n < 0)
                {
                    m_leftOnly.Add(left[l++]);
                }
                else if (n > 0)
                {
                    m_rightOnly.Add(right[r++]);
                }
                else
                {
                    l++;
                    r++;
                }
            }

            for (; l < left.Length; l++)
            {
                m_leftOnly.Add(left[l]);
            }

            for (; r < right.Length; r++)
            {
                m_rightOnly.Add(right[r]);
            }
        }

        ~SymDiff()
        {
            Dispose(false);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!m_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                m_disposed = true;
            }
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_leftOnly.Clear();
                m_rightOnly.Clear();
            }
        }
    }
}

