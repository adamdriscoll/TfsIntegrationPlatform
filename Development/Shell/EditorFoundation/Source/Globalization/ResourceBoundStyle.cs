// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Represents a style that is bound to a set of resources.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// <Window 
    ///   xmlns:eg="clr-namespace:EditorFoundation.Globalization;assembly=EditorFoundation"
    ///   xmlns:ep="clr-namespace:EditorFoundation.Properties;assembly=EditorFoundation">
    /// 
    ///     <Window.Resources>
    ///         <eg:ManagedResourceProvider x:Key="BaseResources" ResourceManager="{x:Static ep:WpfViewResources.ResourceManager}" />
    ///
    ///         <eg:ResourceBoundStyle x:Key="FileMenuItemStyle" ResourceProvider="{StaticResource BaseResources}" ResourceKeyBase="Form1_FileMenu" TargetType="MenuItem" BasedOn="{ev:DeferredStyle {x:Type MenuItem}}">
    ///             <eg:ResourceBoundProperty Property="MenuItem.Header" />
    ///             <eg:ResourceBoundProperty Property="MenuItem.FontWeight" />
    ///             <eg:ResourceBoundProperty Property="MenuItem.FontFamily" />
    ///         </eg:ResourceBoundStyle>
    ///     </Window.Resources>
    ///
    ///     <MenuItem Name="fileMenuItem" Style="{DynamicResource FileMenuItemStyle}">
    ///    
    /// </Window>
    /// ]]>
    /// 
    /// fileMenuItem.Style = new ResourceBoundStyle (managedResourceProvider, "Form1_FileMenu", MenuItem.HeaderProperty, MenuItem.FontWeightProperty, MenuItem.FontFamilyProperty);
    /// 
    /// </example>
    [ContentProperty ("Properties")]
    public class ResourceBoundStyle : Style
    {
        #region Fields
        private ObservableCollection<ResourceBoundProperty> properties;
        private IResourceProvider resourceProvider; //** Delete when we have C# 3.0 support **
        private string resourceKeyBase; //** Delete when we have C# 3.0 support **
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBoundStyle"/> class.
        /// </summary>
        public ResourceBoundStyle ()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBoundStyle"/> class.
        /// </summary>
        /// <param name="resourceProvider">The resource provider.</param>
        /// <param name="resourceKeyBase">The resource key base.</param>
        /// <param name="properties">The properties.</param>
        public ResourceBoundStyle (IResourceProvider resourceProvider, string resourceKeyBase, params ResourceBoundProperty[] properties)
        {
            this.ResourceProvider = resourceProvider;
            this.ResourceKeyBase = resourceKeyBase;

            foreach (ResourceBoundProperty _property in properties)
            {
                this.Properties.Add (_property);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the resource provider.
        /// </summary>
        //public IResourceProvider ResourceProvider { get; set; } ** Uncomment when we have C# 3.0 support **
        public IResourceProvider ResourceProvider
        {
            get
            {
                return this.resourceProvider;
            }
            set
            {
                this.resourceProvider = value;
            }
        }

        /// <summary>
        /// Gets or sets the resource key base.
        /// </summary>
        /// <remarks>
        /// If the ResourceKeyBase is set to Control1, and resource bound properties
        /// are added for PropertyA and PropertyB, then we look for resources named
        /// Control1_PropertyA and Control1_PropertyB.
        /// </remarks>
        //public string ResourceKeyBase { get; set; } ** Uncomment when we have C# 3.0 support **
        public string ResourceKeyBase
        {
            get
            {
                return this.resourceKeyBase;
            }
            set
            {
                this.resourceKeyBase = value;
            }
        }

        /// <summary>
        /// Gets the properties that are resource bound.
        /// </summary>
        public ObservableCollection<ResourceBoundProperty> Properties
        {
            get
            {
                if (this.properties == null)
                {
                    this.properties = new ObservableCollection<ResourceBoundProperty> ();
                    this.properties.CollectionChanged += this.OnPropertiesChanged;
                }
                return this.properties;
            }
        }
        #endregion

        #region Private Methods
        private void OnPropertiesChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ResourceBoundProperty _property in e.OldItems)
                {
                    for (int i = this.Setters.Count - 1; i >= 0; i--)
                    {
                        Setter setter = this.Setters[i] as Setter;
                        if (setter != null && setter.Property == _property.Property)
                        {
                            this.Setters.RemoveAt (i);
                        }
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (ResourceBoundProperty _property in e.NewItems)
                {
                    Binding binding = new Binding (string.Format ("[{0}_{1}]", this.ResourceKeyBase, _property.Property.Name));
                    binding.Source = this.ResourceProvider;
                    binding.Converter = _property.Converter;

                    this.Setters.Add (new Setter (_property.Property, binding));
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a property that is to be bound to a resource.
    /// </summary>
    public class ResourceBoundProperty
    {
        private DependencyProperty _property; //** Delete when we have C# 3.0 support **
        private IValueConverter converter; //** Delete when we have C# 3.0 support **

        /// <summary>
        /// Gets or sets the property.
        /// </summary>
        //public DependencyProperty Property { get; set; } ** Uncomment when we have C# 3.0 support **
        public DependencyProperty Property
        {
            get
            {
                return this._property;
            }
            set
            {
                this._property = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the converter (optional).
        /// </summary>
        //public IValueConverter Converter { get; set; } ** Uncomment when we have C# 3.0 support **
        public IValueConverter Converter
        {
            get
            {
                return this.converter;
            }
            set
            {
                this.converter = value;
            }
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Windows.DependencyProperty"/> to <see cref="ResourceBoundProperty"/>.
        /// </summary>
        /// <param name="dependencyProperty">The dependency property.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator ResourceBoundProperty (DependencyProperty dependencyProperty)
        {
            //return new ResourceBoundProperty { Property = dependencyProperty }; ** Uncomment when we have C# 3.0 support **
            ResourceBoundProperty resourceBoundProperty = new ResourceBoundProperty ();
            resourceBoundProperty.Property = dependencyProperty;
            return resourceBoundProperty;
        }
    }
}
