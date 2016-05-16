// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Represents a converter that converts a string and a set of format arguments to a formatted string.
    /// </summary>
    public class StringFormatConverter : IMultiValueConverter
    {
        #region Fields
        private static readonly StringFormatConverter _default = new StringFormatConverter ();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the default <see cref="StringFormatConverter"/>.
        /// </summary>
        public static StringFormatConverter Default
        {
            get
            {
                return StringFormatConverter._default;
            }
        }
        #endregion

        #region Public Methods
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
            string formatString = parameter as string;
            object[] formatArgs = values;

            if (string.IsNullOrEmpty (formatString))
            {
                if (values != null && values.Length > 0)
                {
                    formatString = values[0] as string;
                    
                    if (!string.IsNullOrEmpty (formatString))
                    {
                        formatArgs = new object[values.Length - 1];
                        Array.Copy (values, 1, formatArgs, 0, formatArgs.Length);
                    }
                }
            }

            if (string.IsNullOrEmpty (formatString))
            {
                return string.Empty;
            }

            return string.Format (formatString, formatArgs);
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
    }
}
