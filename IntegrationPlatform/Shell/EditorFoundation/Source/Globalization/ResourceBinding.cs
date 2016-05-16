// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Implements a <see cref="MarkupExtension"/> that provides an alternative way of binding properties to resources.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// <Window 
    ///   xmlns:eg="clr-namespace:EditorFoundation.Globalization;assembly=EditorFoundation"
    ///   xmlns:ep="clr-namespace:EditorFoundation.Properties;assembly=EditorFoundation">
    /// 
    ///   <Grid>
    ///     <eg:ResourceBinding.Provider>
    ///       <eg:ManagedResourceProvider ResourceManager="{x:Static ep:Resources.ResourceManager}" />
    ///     </eg:ResourceBinding.Provider>
    ///     
    ///     <Label Content="{eg:ResourceBinding New}" />
    ///     <Label Content="{eg:ResourceBinding Save}" />
    ///   </Grid>
    /// 
    /// </Window>
    /// ]]>
    /// </example>
    public class ResourceBinding : MarkupExtension
    {
        #region Fields
        private readonly string resourceKey;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBinding"/> class.
        /// </summary>
        /// <param name="resourceKey">The resource key.</param>
        public ResourceBinding (string resourceKey)
        {
            this.resourceKey = resourceKey;
        }
        #endregion

        #region Dependency Properties
        /// <summary>
        /// Identifies the Provider attached property.
        /// </summary>
        public static readonly DependencyProperty ProviderProperty =
            DependencyProperty.RegisterAttached ("Provider", typeof (IResourceProvider), typeof (ResourceBinding), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Gets the resource provider for the specified <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <returns>The resource provider.</returns>
        public static IResourceProvider GetProvider (DependencyObject dependencyObject)
        {
            return (IResourceProvider)dependencyObject.GetValue (ResourceBinding.ProviderProperty);
        }

        /// <summary>
        /// Sets the resource provider for the specified <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <param name="provider">The resource provider.</param>
        public static void SetProvider (DependencyObject dependencyObject, IResourceProvider provider)
        {
            dependencyObject.SetValue (ResourceBinding.ProviderProperty, provider);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns an object that is set as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">Object that can provide services for the markup extension.</param>
        /// <returns>
        /// The object value to set on the property where the extension is applied.
        /// </returns>
        public override object ProvideValue (IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty (this.resourceKey))
            {
                throw new InvalidOperationException ("ResourceKey must be specified.");
            }

            // Get the provide value target service provider
            IProvideValueTarget provideValueTarget = (IProvideValueTarget)serviceProvider.GetService (typeof (IProvideValueTarget));
            if (provideValueTarget != null)
            {
                // Get the framework element being targeted
                FrameworkElement frameworkElement = provideValueTarget.TargetObject as FrameworkElement;

                if (frameworkElement != null)
                {
                    // Get the resource provider for this framework element
                    IResourceProvider resourceProvider = ResourceBinding.GetProvider (frameworkElement);

                    if (resourceProvider != null)
                    {
                        // Create a binding against the resource provider
                        Binding binding = new Binding (string.Format ("[{0}]", this.resourceKey));
                        binding.Source = resourceProvider;
                        return binding.ProvideValue (serviceProvider);
                    }
                }
            }

            return null;
        }
        #endregion
    }
}
