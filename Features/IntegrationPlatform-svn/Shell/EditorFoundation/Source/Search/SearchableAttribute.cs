// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.Migration.Shell.Search
{
    /// <summary>
    /// Allows Model implementations to specify custom Keyword Providers
    /// or exclude certain types or properties from searches.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Property)]
    public class SearchableAttribute : Attribute
    {
        #region Fields
        /// <summary>
        /// Specifies the default value for the SearchableAttribute.
        /// </summary>
        /// <remarks>
        /// The default value specifies that the member is searchable,
        /// and that the DefaultKeywordProvider should be used.
        /// </remarks>
        public static readonly SearchableAttribute Default;

        private static readonly Dictionary<Type, IKeywordProvider> keywordProviders;
        private readonly IKeywordProvider keywordProvider;
        #endregion

        #region Constructors
        static SearchableAttribute ()
        {
            SearchableAttribute.Default = new SearchableAttribute (true);
            SearchableAttribute.keywordProviders = new Dictionary<Type, IKeywordProvider> ();
        }

        /// <summary>
        /// Creates a new instance of the Searchable Attribute.
        /// </summary>
        /// <param name="searchable"><c>true</c> if the property or type should be included in searches, <c>false</c> otherwise.</param>
        public SearchableAttribute (bool searchable)
        {
            if (searchable)
            {
                this.keywordProvider = new DefaultKeywordProvider ();
            }
        }

        /// <summary>
        /// Creates a new instance of the Searchable Attribute.
        /// </summary>
        /// <param name="keywordProviderType">The type of the Keyword Provider that should be used for the type or property.</param>
        public SearchableAttribute (Type keywordProviderType)
        {
            if (!SearchableAttribute.keywordProviders.TryGetValue (keywordProviderType, out this.keywordProvider))
            {
                if (!typeof (IKeywordProvider).IsAssignableFrom (keywordProviderType))
                {
                    throw new ArgumentException (string.Format ("{0} does not implement {1}", keywordProviderType.Name, typeof (IKeywordProvider).Name));
                }
                this.keywordProvider = (IKeywordProvider)Activator.CreateInstance (keywordProviderType);
                SearchableAttribute.keywordProviders[keywordProviderType] = this.keywordProvider;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the Keyword Provider instance.
        /// </summary>
        public IKeywordProvider KeywordProvider
        {
            get
            {
                return this.keywordProvider;
            }
        }
        #endregion
    }
}
