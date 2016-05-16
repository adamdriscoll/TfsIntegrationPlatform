// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Represents a converter that evaluates a property path for an object.
    /// </summary>
    public class PropertyPathConverter : IValueConverter, IMultiValueConverter
    {
        #region Fields
        private static readonly PropertyPathConverter _default = new PropertyPathConverter ();
        private object source; //** Delete when we have C# 3.0 support **
        private string pathFormatString; //** Delete when we have C# 3.0 support **
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyPathConverter"/> class.
        /// </summary>
        public PropertyPathConverter ()
        {
            this.PathFormatString = "{0}";
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the default <see cref="PropertyPathConverter"/>.
        /// </summary>
        public static PropertyPathConverter Default
        {
            get
            {
                return PropertyPathConverter._default;
            }
        }

        /// <summary>
        /// Gets or sets the binding source.
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
        /// Gets or sets the path format string.
        /// </summary>
        //public string PathFormatString { get; set; } ** Uncomment when we have C# 3.0 support **
        public string PathFormatString
        {
            get
            {
                return this.pathFormatString;
            }
            set
            {
                this.pathFormatString = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
        {
            return PropertyPathConverter.Convert (this.Source, this.PathFormatString, value);
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException ();
        }

        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding"/> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue"/> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty"/>.<see cref="F:System.Windows.DependencyProperty.UnsetValue"/> indicates that the converter did not produce a value, and that the binding will use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue"/> if it is available, or else will use the default value.A return value of <see cref="T:System.Windows.Data.Binding"/>.<see cref="F:System.Windows.Data.Binding.DoNothing"/> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue"/> or the default value.
        /// </returns>
        public object Convert (object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return PropertyPathConverter.Convert (this.Source, this.PathFormatString, values);
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <param name="value">The value that the binding target produces.</param>
        /// <param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// An array of values that have been converted from the target value back to the source values.
        /// </returns>
        public object[] ConvertBack (object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException ();
        }
        #endregion

        #region Private Methods
        private static object Convert (object source, string pathFormatString, params object[] values)
        {
            if (source == null)
            {
                throw new InvalidOperationException ("Source not set.");
            }

            if (values.Length == 0)
            {
                throw new ArgumentException ("No values specified.", "values");
            }

            //return source.EvaluatePropertyPath (pathFormatString, values); ** Uncomment when we have C# 3.0 support **
            return Extensions.EvaluatePropertyPath (source, pathFormatString, values);
        }
        #endregion
    }
}
