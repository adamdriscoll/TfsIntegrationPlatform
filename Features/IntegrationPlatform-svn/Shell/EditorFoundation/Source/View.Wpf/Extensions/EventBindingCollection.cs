// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Represents an ordered collection of <see cref="EventBinding"/> objects.
    /// </summary>
    public class EventBindingCollection : Collection<EventBinding>
    {
        #region Fields
        private UIElement sourceUIElement;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the source <see cref="UIElement"/>.
        /// </summary>
        protected internal UIElement SourceUIElement
        {
            get
            {
                return this.sourceUIElement;
            }
            set
            {
                this.sourceUIElement = value;

                // Update the source for each EventBinding
                foreach (EventBinding eventBinding in this)
                {
                    eventBinding.Source = value;
                }
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Replaces the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index. The value can be null for reference types.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is less than zero.-or-<paramref name="index"/> is greater than <see cref="P:System.Collections.ObjectModel.Collection`1.Count"/>.</exception>
        protected override void SetItem (int index, EventBinding item)
        {
            EventBindingCollection.ValidateValue (item);
            if (item != this[index])
            {
                this[index].Source = null;
                base.SetItem (index, item);
                this[index].Source = this.SourceUIElement;
            }
        }

        /// <summary>
        /// Inserts an element into the <see cref="T:System.Collections.ObjectModel.Collection`1"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert. The value can be null for reference types.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is less than zero.-or-<paramref name="index"/> is greater than <see cref="P:System.Collections.ObjectModel.Collection`1.Count"/>.</exception>
        protected override void InsertItem (int index, EventBinding item)
        {
            EventBindingCollection.ValidateValue (item);
            base.InsertItem (index, item);
            item.Source = this.SourceUIElement;
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="T:System.Collections.ObjectModel.Collection`1"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is less than zero.-or-<paramref name="index"/> is equal to or greater than <see cref="P:System.Collections.ObjectModel.Collection`1.Count"/>.</exception>
        protected override void RemoveItem (int index)
        {
            this[index].Source = null;
            base.RemoveItem (index);
        }

        /// <summary>
        /// Removes all elements from the <see cref="T:System.Collections.ObjectModel.Collection`1"/>.
        /// </summary>
        protected override void ClearItems ()
        {
            while (this.Count > 0)
            {
                this.RemoveAt (0);
            }
        }
        #endregion

        #region Private Methods
        private static void ValidateValue (EventBinding eventBinding)
        {
            if (eventBinding == null)
            {
                throw new NotSupportedException ("The specified value is not supported by the EventBindingCollection.");
            }
        }
        #endregion
    }
}