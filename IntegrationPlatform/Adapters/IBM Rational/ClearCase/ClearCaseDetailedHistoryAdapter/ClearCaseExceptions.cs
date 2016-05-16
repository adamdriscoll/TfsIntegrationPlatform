// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    //********************************************************************************************
    /// <summary>
    /// INTERNAL Exception thrown when something is wrong with the syntax of a path.  It derives
    /// from ArgumentException to match the exception used by System.IO.Path.
    /// </summary>
    //********************************************************************************************
    [Serializable]
    public class InvalidPathException : ArgumentException
    {
        public InvalidPathException(String message)
            : base(message)
        {
        }
        public InvalidPathException(string message, Exception exception)
            : base(message, exception)
        {
        }
        protected InvalidPathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    //********************************************************************************************
    /// <summary>
    /// INTERNAL Exception thrown when an element cannot be found. Either the element does not exist
    /// in clearcase server or the item is not visible in the view.
    /// </summary>
    //********************************************************************************************
    [Serializable]
    public class ElementNotFoundException : ArgumentException
    {
        public ElementNotFoundException(string elementPath)
            :base(string.Format(CCResources.ElementNotFoundException, elementPath))
        {
        }
    }

    //********************************************************************************************
    /// <summary>
    /// Exception thrown when a ClearTool command fails
    /// </summary>
    //********************************************************************************************

    [Serializable]
    public class ClearToolCommandException : Exception
    {
        public ClearToolCommandException(String message)
            : base(message)
        {
        }
        public ClearToolCommandException(string message, Exception exception)
            : base(message, exception)
        {
        }
        protected ClearToolCommandException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    //********************************************************************************************
    /// <summary>
    /// INTERNAL Exception thrown when an action is not supported by the clearcase migration provider
    /// </summary>
    //********************************************************************************************
    [Serializable]
    public class ChangeActionNotSupportedException : ArgumentException
    {
        public ChangeActionNotSupportedException(Guid changeActionId)
            : base(string.Format(CCResources.ChangeActionNotSupportedException, changeActionId))
        {
        }
    }
}
