// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows.Markup;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Implements a data structure for describing a property as a path below another property, or below an owning type.
    /// </summary>
    public class PropertyPath : MarkupExtension
    {
        #region Fields
        private object source; //** Delete when we have C# 3.0 support **
        private string path; //** Delete when we have C# 3.0 support **
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the object to use as the property path source.
        /// </summary>
        //public object Source { get; set; } ** Uncomment when we have C# 3.0 support **
        public object Source
        {
            get
            {
                return this.source;
            }
            set
            {
                this.source = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the path to the property.
        /// </summary>
        //public string Path { get; set; } ** Uncomment when we have C# 3.0 support **
        public string Path
        {
            get
            {
                return this.path;
            }
            set
            {
                this.path = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns the value of the property path when evaluated against the source object.
        /// </summary>
        /// <param name="serviceProvider">Object that can provide services for the markup extension.</param>
        /// <returns>
        /// The evaulated property path value.
        /// </returns>
        public override object ProvideValue (IServiceProvider serviceProvider)
        {
            //return this.Source.EvaluatePropertyPath (this.Path); ** Uncomment when we have C# 3.0 support **
            return Extensions.EvaluatePropertyPath (this.Source, this.Path);
        }
        #endregion
    }
}
