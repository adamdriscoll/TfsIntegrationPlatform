// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public enum PlatformCommentSuffixType
    {
        Minimal,
        Verbose
    }

    /// <summary>
    /// The interface for WIT diff items.
    /// </summary>
    public interface ICommentDecorationService
    {
        /// <summary>
        /// Get the suffix to be appended to ChangeGroup comments for ChangeGroups migrated by the Integration Platform.
        /// The returned string may or may not include the arguments passed in depending on the Session configuration setting for PlatformCommentSuffixType,
        /// and will include any CustomCommentSuffix specified in the Session configuration
        /// </summary>
        /// <param name="id">An Id that identifies corresponding item on the peer server</param>
        /// <returns></returns>
        string GetChangeGroupCommentSuffix(string id);

        /// <summary>
        /// Allows the caller to include additional text in the comment suffix, but under the control of 
        /// the PlatformCommentSuffixType: if the value is Minimal, the additionalText will not be appended.
        /// </summary>
        /// <param name="currentCommentOrSuffix">Either the current comment string or just the comment suffix</param>
        /// <param name="additionalText">Additional text to append to the currentCommentOrSuffix unless PlatformCommentSuffixType is Minimal</param>
        /// <returns>The value of currentCommentOrSuffix, with the value of additionalText appended to it (unless PlatformCommentSuffixType is Minimal)</returns>
        string AddToChangeGroupCommentSuffix(string currentCommentOrSuffix, string additionalText);

        /// <summary>
        /// Returns true if and only if the comment string argument contains an Integration Platform comment suffix
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        bool HasPlatformCommentSuffix(string comment);
    }

}
