// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Represents a converter that converts <see cref="Boolean"/> values to and from numeric values.
    /// </summary>
    public class BooleanToNumberConverter : IValueConverter
    {
        #region Fields
        private static readonly BooleanToNumberConverter _default = new BooleanToNumberConverter ();
        private double defaultFalseValue; //** Delete when we have C# 3.0 support **
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanToNumberConverter"/> class.
        /// </summary>
        public BooleanToNumberConverter () : this (0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanToNumberConverter"/> class.
        /// </summary>
        /// <param name="defaultFalseValue">The default numeric value for the false boolean value.</param>
        public BooleanToNumberConverter (double defaultFalseValue)
        {
            this.DefaultFalseValue = defaultFalseValue;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the default <see cref="BooleanToNumberConverter"/>.
        /// </summary>
        public static BooleanToNumberConverter Default
        {
            get
            {
                return BooleanToNumberConverter._default;
            }
        }

        /// <summary>
        /// Gets or sets the default numeric value for the false boolean value.
        /// </summary>
        //public double DefaultFalseValue { get; set; } ** Uncomment when we have C# 3.0 support **
        public double DefaultFalseValue
        {
            get
            {
                return this.defaultFalseValue;
            }
            set
            {
                this.defaultFalseValue = value;
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
        public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return this.Convert (value, targetType, parameter);
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
        public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return this.Convert (value, targetType, parameter);
        }
        #endregion

        #region Private Methods
        private object Convert (object value, Type targetType, object parameter)
        {
            double falseValue = this.DefaultFalseValue;
            if (parameter is double)
            {
                falseValue = (double)parameter;
            }

            bool boolValue = false;
            if (value is bool)
            {
                boolValue = (bool)value;
            }
            else if (value is bool?)
            {
                bool? nullable = (bool?)value;
                boolValue = nullable.HasValue ? nullable.Value : false;
            }

            double doubleValue = falseValue;
            if (value is double)
            {
                doubleValue = (double)value;
            }

            if (targetType == typeof (bool))
            {
                return doubleValue == falseValue ? false : true;
            }
            else if (targetType == typeof (bool?))
            {
                return doubleValue == falseValue ? new Nullable<bool> (false) : new Nullable<bool> (true);
            }
            else
            {
                doubleValue = boolValue ? 1.0 : falseValue;
                return System.Convert.ChangeType (doubleValue, targetType);
            }
        }
        #endregion
    }
}
