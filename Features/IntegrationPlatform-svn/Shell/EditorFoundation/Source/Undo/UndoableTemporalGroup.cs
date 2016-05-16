// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Undo
{
    /// <summary>
    /// An UndoableGroup that remains open until no change is made
    /// for a specified interval.
    /// </summary>
    public class UndoableTemporalGroup : UndoableGroup
    {
        #region Fields
        private TimeSpan groupingTimeout;
        private DateTime lastAddTime;
        #endregion

        #region Constructors & Destructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UndoableTemporalGroup"/> class.
        /// </summary>
        /// <param name="timeout">The timeout for the temporal group.</param>
        public UndoableTemporalGroup (TimeSpan timeout)
            : this (timeout, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoableTemporalGroup"/> class.
        /// </summary>
        /// <param name="timeout">The timeout for the temporal group.</param>
        /// <param name="startImmediately">if set to <c>true</c> start the timeout timer immediately.</param>
        public UndoableTemporalGroup (TimeSpan timeout, bool startImmediately)
            : base ()
        {
            this.groupingTimeout = timeout;
            if (startImmediately)
            {
                this.lastAddTime = DateTime.Now;
            }
            else
            {
                this.lastAddTime = DateTime.MaxValue;
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Adds the specified IUndoable to this UndoableGroup.
        /// </summary>
        /// <param name="undoable">The IUndoable to add to this UndoableGroup.</param>
        protected override void AddUndoable (IUndoable undoable)
        {
            this.lastAddTime = DateTime.Now;
            base.AddUndoable (undoable);
        }

        /// <summary>
        /// Determines whether this UndoableGroup can contain the specified IUndoable directly.
        /// </summary>
        /// <param name="undoable">The IUndoable to check for containability.</param>
        /// <returns>
        /// 	<c>true</c> if the specified IUndoable can be contained by this UndoableGroup, <c>false</c> otherwise.
        /// </returns>
        protected override bool CanContainUndoable (IUndoable undoable)
        {
            if (DateTime.Now.Subtract (this.lastAddTime) <= groupingTimeout)
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}
