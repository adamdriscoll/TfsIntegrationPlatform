// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Microsoft.TeamFoundation.Migration.Shell.Globalization
{
    /// <summary>
    /// Represents a converter that converts to and from various image formats.
    /// </summary>
    public class ImageConverter : IValueConverter
    {
        #region Fields
        private static readonly ImageConverter defaultInstance = new ImageConverter ();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the default <see cref="ImageConverter"/> instance.
        /// </summary>
        public static ImageConverter Default
        {
            get
            {
                return ImageConverter.defaultInstance;
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
            if (value == null)
            {
                return null;
            }

            if (value is byte[])
            {
                byte[] data = (byte[])value;
                if (targetType.IsAssignableFrom (typeof (BitmapFrame)))
                {
                    using (MemoryStream stream = new MemoryStream (data))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();

                        bitmapImage.Freeze();

                        return bitmapImage;
                    }
                }
                else
                {
                    throw new NotSupportedException (string.Format ("Conversion from {0} to {1} is not supported.", value.GetType ().FullName, targetType.FullName));
                }
            }
            else
            {
                throw new NotSupportedException (string.Format ("Conversion from {0} is not supported.", value.GetType ().FullName));
            }
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
        #endregion
    }
}
