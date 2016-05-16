// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows;
using System.Windows.Markup;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    // TODO: Could this be DeferredResource, and work like this:
    // 1. IServiceProvider is used to get the target object/property, which are passed to DeferredResourcePlaceholder
    // 2. Either store static list of constructed DeferredResourcePlaceholders, or somehow get this into IServiceProvider
    // 3. After loading the dictionary, resolve all the DeferredResourcePlaceholders that were created while loading the dictionary

    //public class DeferredResource : MarkupExtension
    //{
    //     #region Fields
    //    private readonly object resourceKey;
    //    private static readonly Dictionary<PropertyReference, object> deferredReferences;
    //    #endregion

    //    #region Constructors
    //    static DeferredResource ()
    //    {
    //        DeferredResource.deferredReferences = new Dictionary<PropertyReference, object> ();
    //    }

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="DeferredResource"/> class.
    //    /// </summary>
    //    /// <param name="resourceKey">The resource key.</param>
    //    public DeferredResource (object resourceKey)
    //    {
    //        this.resourceKey = resourceKey;
    //    }
    //    #endregion

    //    public override object ProvideValue (IServiceProvider serviceProvider)
    //    {
    //        if (serviceProvider == null)
    //        {
    //            throw new ArgumentNullException ("serviceProvider", "DeferredResource markup extension requires a service provider.");
    //        }

    //        IProvideValueTarget provideValueTargetService = serviceProvider.GetService (typeof (IProvideValueTarget)) as IProvideValueTarget;
    //        if (provideValueTargetService == null)
    //        {
    //            throw new InvalidOperationException ("The service provider does not contain IProvideValueTarget service.");
    //        }
            
    //        PropertyReference propertyReference = PropertyReference.Create (provideValueTargetService.TargetObject, provideValueTargetService.TargetProperty);
    //        DeferredResource.deferredReferences[propertyReference] = this.resourceKey;

    //        return propertyReference.DefaultValue;
    //    }

    //    public static void ResolveDeferredResources (ResourceDictionary resourceDictionary)
    //    {
    //        foreach (KeyValuePair<PropertyReference, object> deferredReference in DeferredResource.deferredReferences)
    //        {
    //            PropertyReference propertyReference = deferredReference.Key;
    //            object resourceKey = deferredReference.Value;

    //            propertyReference.SetPropertyValue (resourceDictionary.FindResource (resourceKey));
    //        }

    //        DeferredResource.deferredReferences.Clear ();
    //    }

    //    private abstract class PropertyReference
    //    {
    //        #region Fields
    //        private readonly object target;
    //        private readonly object property;
    //        #endregion

    //        protected PropertyReference (object target, object property)
    //        {
    //            this.target = target;
    //            this.property = property;
    //        }

    //        public object Target
    //        {
    //            get
    //            {
    //                return this.target;
    //            }
    //        }

    //        public object Property
    //        {
    //            get
    //            {
    //                return this.property;
    //            }
    //        }

    //        public abstract object DefaultValue
    //        {
    //            get;
    //        }

    //        public static PropertyReference Create (object target, object property)
    //        {
    //            PropertyInfo propertyInfo = property as PropertyInfo;
    //            if (propertyInfo != null)
    //            {
    //                return new ClrPropertyReference (target, propertyInfo);
    //            }
    //            else
    //            {
    //                return new DependencyPropertyReference ((DependencyObject)target, (DependencyProperty)property);
    //            }
    //        }

    //        public abstract void SetPropertyValue (object value);

    //        public override bool Equals (object obj)
    //        {
    //            PropertyReference propertyReference = obj as PropertyReference;
    //            return (propertyReference != null && propertyReference.Target == this.Target && propertyReference.Property == this.Property);
    //        }

    //        public override int GetHashCode ()
    //        {
    //            unchecked
    //            {
    //                return this.Target.GetHashCode () + this.Property.GetHashCode ();
    //            }
    //        }
    //    }

    //    private sealed class ClrPropertyReference : PropertyReference
    //    {
    //        public ClrPropertyReference (object target, PropertyInfo property)
    //            : base (target, property)
    //        {
    //        }

    //        public new PropertyInfo Property
    //        {
    //            get
    //            {
    //                return (PropertyInfo)base.Property;
    //            }
    //        }

    //        public override object DefaultValue
    //        {
    //            get
    //            {
    //                if (this.Property.PropertyType.IsValueType)
    //                {
    //                    return Activator.CreateInstance (this.Property.PropertyType);
    //                }
    //                else
    //                {
    //                    return null;
    //                }
    //            }
    //        }

    //        public override void SetPropertyValue (object value)
    //        {
    //            this.Property.SetValue (this.Target, value, null);
    //        }

    //        public override string ToString ()
    //        {
    //            return this.Target.GetType ().FullName + "." + this.Property.Name;
    //        }
    //    }

    //    private sealed class DependencyPropertyReference : PropertyReference
    //    {
    //        public DependencyPropertyReference (DependencyObject target, DependencyProperty property)
    //            : base (target, property)
    //        {
    //        }

    //        public new DependencyObject Target
    //        {
    //            get
    //            {
    //                return (DependencyObject)base.Target;
    //            }
    //        }

    //        public new DependencyProperty Property
    //        {
    //            get
    //            {
    //                return (DependencyProperty)base.Property;
    //            }
    //        }

    //        public override object DefaultValue
    //        {
    //            get
    //            {
    //                return DependencyProperty.UnsetValue;
    //            }
    //        }

    //        public override void SetPropertyValue (object value)
    //        {
    //            this.Target.SetValue (this.Property, value);
    //        }

    //        public override string ToString ()
    //        {
    //            return this.Target.GetType ().FullName + "." + this.Property.Name;
    //        }
    //    }
    //}



    /// <summary>
    /// Represents a style that is late bound.
    /// </summary>
    public class DeferredStyle : MarkupExtension
    {
        #region Fields
        private readonly object styleKey;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredStyle"/> class.
        /// </summary>
        /// <param name="styleKey">The style key.</param>
        public DeferredStyle (object styleKey)
        {
            this.styleKey = styleKey;
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
            return new DeferredStylePlaceholder (this.styleKey);
        }

        /// <summary>
        /// Resolves all deferred styles in the specified <see cref="ResourceDictionary"/>.
        /// </summary>
        /// <param name="resources">The resources potentially containing deferred styles.</param>
        public static void ResolveDeferredStyles (ResourceDictionary resources)
        {
            //foreach (ResourceDictionary resourceDictionary in resources.EnumerateResourceDictionaries ()) ** Uncomment when we have C# 3.0 support **
            foreach (ResourceDictionary resourceDictionary in Extensions.EnumerateResourceDictionaries (resources))
            {
                foreach (System.Collections.DictionaryEntry resource in resourceDictionary)
                {
                    if (resource.Value != null && resource.Value is Style)
                    {
                        Style derivedStyle = (Style)resource.Value;
                        if (!derivedStyle.IsSealed && derivedStyle.BasedOn is DeferredStylePlaceholder)
                        {
                            DeferredStylePlaceholder basedOn = (DeferredStylePlaceholder)derivedStyle.BasedOn;
                            //Style baseStyle = resources.FindResource (basedOn.StyleKey) as Style; ** Uncomment when we have C# 3.0 support **
                            Style baseStyle = Extensions.FindResource (resources, basedOn.StyleKey) as Style;
                            if (baseStyle != null)
                            {
                                derivedStyle.BasedOn = baseStyle;
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }

    internal class DeferredStylePlaceholder : Style
    {
        #region Fields
        private readonly object styleKey;
        #endregion

        #region Constructors
        public DeferredStylePlaceholder (object styleKey)
        {
            this.styleKey = styleKey;
        }
        #endregion

        #region Properties
        public object StyleKey
        {
            get
            {
                return this.styleKey;
            }
        }
        #endregion
    }
}
