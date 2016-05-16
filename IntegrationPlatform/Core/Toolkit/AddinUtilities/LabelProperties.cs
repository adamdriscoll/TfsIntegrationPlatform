// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This class encapsulates
    /// </summary>
    public class LabelProperties : FileMetadataProperties
    {
        public const string LabelNameKey = "LabelName";
        public const string LabelCommentKey = "LabelComment";
        public const string LabelOwnerKey = "LabelOwner";
        public const string LabelScopeKey = "LabelScope";

        /// <summary>
        /// Construct an object that encapsulates the properties of the Migration action of content type 
        /// </summary>
        /// <param name="label">A class imlementing the ILable interface</param>
        public LabelProperties(ILabel label)
            : base()
        {
            this.Add(LabelNameKey, label.Name);
            this.Add(LabelCommentKey, label.Comment);
            this.Add(LabelOwnerKey, label.OwnerName);
            this.Add(LabelScopeKey, label.Scope);
        }

        public LabelProperties(FileMetadataProperties basePropertiesClass)
        {
            foreach (string key in basePropertiesClass.Keys)
            {
                this.Add(key, basePropertiesClass[key]);
            }
        }
    }
}
