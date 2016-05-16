// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;

namespace Microsoft.TeamFoundation.Migration.Shell.Extensibility
{
    /// <summary>
    /// Indicates the level of support for a Plugin context.
    /// </summary>
    public enum ContextSupport : byte
    {
        /// <summary>
        /// Indicates that the Plugin context is optional.
        /// </summary>
        Optional = 0x1,

        /// <summary>
        /// Indicates that the Plugin context is required.
        /// </summary>
        Required = 0x2
    }

    /// <summary>
    /// Used to specify a Context that a Plugin supports.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class, AllowMultiple=true)]
    public class PluginContextAttribute : Attribute
    {
        #region Fields
        private readonly Type contextType;
        private readonly ContextSupport contextSupport;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the PluginContextAttribute class.
        /// </summary>
        /// <param name="contextType">Specifies the type of the contextual object that is supported by this Plugin.</param>
        /// <param name="contextSupport">Specifies the level of support for the specified contextual object type.</param>
        public PluginContextAttribute (Type contextType, ContextSupport contextSupport)
        {
            this.contextType = contextType;
            this.contextSupport = contextSupport;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the type of the Context.
        /// </summary>
        public Type ContextType
        {
            get
            {
                return this.contextType;
            }
        }

        /// <summary>
        /// Specifies whether the Context is optional or required.
        /// </summary>
        public ContextSupport ContextSupport
        {
            get
            {
                return this.contextSupport;
            }
        }
        #endregion
    }
}
