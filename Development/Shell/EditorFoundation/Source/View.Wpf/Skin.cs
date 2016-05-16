// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Contains the name and resources that define a skin.
    /// </summary>
    public class Skin
    {
        #region Fields
        private readonly string name;
        private readonly ResourceDictionary resources;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Skin"/> class.
        /// </summary>
        /// <param name="name">The name of the skin.</param>
        /// <param name="resources">The resources for the skin.</param>
        public Skin (string name, ResourceDictionary resources)
        {
            this.name = name;
            this.resources = resources;
            //this.resources.ResolveDeferredStyles ();
            Extensions.ResolveDeferredStyles (this.resources);

            //this.resources.ResolveDeferredResources ();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the name of the skin.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets the resources for the skin.
        /// </summary>
        public ResourceDictionary Resources
        {
            get
            {
                return this.resources;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString ()
        {
            return this.Name;
        }

        /// <summary>
        /// Loads a  <see cref="Skin"/> from the specified path.
        /// </summary>
        /// <param name="path">The skin path.</param>
        /// <returns>The loaded skin.</returns>
        public static Skin Load (string path)
        {
            if (Path.GetExtension (path).Equals (".xaml", StringComparison.OrdinalIgnoreCase))
            {
                return Skin.LoadFromXaml (path);
            }
            else
            {
                throw new ArgumentException (string.Format ("{0} is an unsupported skin file.", Path.GetFileName (path)), "path");
            }
        }
        #endregion

        #region Private Methods
        private static Skin LoadFromXaml (string path)
        {
            ResourceDictionary resources = new ResourceDictionary ();
            resources.Source = new Uri (path, UriKind.Absolute);

            string name = Skin.GetSkinName (resources, path);
            return new Skin (name, resources);
        }

        private static string GetSkinName (ResourceDictionary resources, string path)
        {
            // Try to get the skin name from the resources
            string name = resources["SkinName"] as string;

            // Use the file name as the skin name
            if (string.IsNullOrEmpty (name))
            {
                name = Path.GetFileNameWithoutExtension (path);
            }

            return name;
        }
        #endregion
    }
}
