// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ErrorManagement
{
    /// <summary>
    /// The base class of all ErrorSignatures
    /// </summary>
    /// <remarks>The ICompable implementatin is expected to sort signature with more applicable one in the front,
    /// i.e. netagive int should be returned if obj is less applicable</remarks>
    public abstract class ErrorSignatureBase : IComparable, IEqualityComparer<ErrorSignatureBase>
    {
        public const string WildcardAny = "*";

        public abstract bool Matches(Exception e);

        #region IComparable Members

        public abstract int CompareTo(object obj);

        #endregion

        #region IEqualityComparer<ErrorSignatureBase> Members

        public virtual bool Equals(ErrorSignatureBase x, ErrorSignatureBase y)
        {
            return 0 == x.CompareTo(y);
        }

        public virtual int GetHashCode(ErrorSignatureBase obj)
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
