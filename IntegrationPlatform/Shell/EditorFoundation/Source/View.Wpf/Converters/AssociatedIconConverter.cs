// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Indicates the size of the icon to retrieve.
    /// </summary>
    public enum IconSize : uint
    {
        /// <summary>
        /// A 16x16 icon.
        /// </summary>
        Small = AssociatedIconConverter.SHIL.SMALL,

        /// <summary>
        /// A 32x32 icon.
        /// </summary>
        Medium = AssociatedIconConverter.SHIL.LARGE,

        /// <summary>
        /// A 48x48 icon.
        /// </summary>
        Large = AssociatedIconConverter.SHIL.EXTRALARGE,

        /// <summary>
        /// A 256x256 icon.
        /// </summary>
        ExtraLarge = AssociatedIconConverter.SHIL.JUMBO
    }

    /// <summary>
    /// Represents a converter that converts a file path to an icon that is associated with that file.
    /// </summary>
    public class AssociatedIconConverter : IValueConverter
    {
        #region Fields
        private static readonly AssociatedIconConverter _default = new AssociatedIconConverter ();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the default <see cref="AssociatedIconConverter"/>.
        /// </summary>
        public static AssociatedIconConverter Default
        {
            get
            {
                return AssociatedIconConverter._default;
            }
        }

        private static IconSize DefaultIconSize
        {
            get
            {
                // ExtraLarge (256x256) icons are only supported on Vista and later
                if (Environment.OSVersion.Version.Major < 6)
                {
                    return IconSize.Large;
                }
                else
                {
                    return IconSize.ExtraLarge;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns></returns>
        public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;
            IconSize iconSize = AssociatedIconConverter.DefaultIconSize;

            if (parameter is IconSize)
            {
                iconSize = (IconSize)parameter;
            }

            if (!string.IsNullOrEmpty (path))
            {
                try
                {
                    return AssociatedIconConverter.GetIconFromPath (path, iconSize);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine (string.Format ("Failed to get icon from {0}: {1}{2}", path, Environment.NewLine, exception.ToString ()));
                }
            }

            return null;
        }

        /// <summary>
        /// Converts the back.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns></returns>
        public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException ();
        }

        /// <summary>
        /// Gets the icon for the file at the specified path.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="iconSize">The size of the icon to retrieve.</param>
        /// <returns>An image representing the icon.</returns>
        public static ImageSource GetIconFromPath (string filePath, IconSize iconSize)
        {
            // Get an image list
            Guid iidImageList = new Guid (IID_IImageList);
            IntPtr imageListHandle = IntPtr.Zero;
            int result = SHGetImageList ((int)iconSize, ref iidImageList, ref imageListHandle);
            if (result != 0)
            {
                throw new COMException ("SHGetImageList failed.", result);
            }

            // Get a shell file info for the specified file path
            SHFILEINFO shellFileInfo = new SHFILEINFO ();
            SHGetFileInfo (filePath, 0, ref shellFileInfo, (uint)Marshal.SizeOf (shellFileInfo), (uint)SHGFI.SYSICONINDEX);

            // Get the icon index
            int iconIndex = shellFileInfo.iIcon;

            // Get an icon handle
            IntPtr iconHandle = ImageList_GetIcon (imageListHandle, iconIndex, (uint)ILD.TRANSPARENT);
            if (iconHandle == IntPtr.Zero)
            {
                throw new Exception ("ImageList_GetIcon failed.");
            }

            // Write the icon (as a png) into a memory stream
            MemoryStream memoryStream = new MemoryStream ();
            Icon.FromHandle (iconHandle).ToBitmap ().Save (memoryStream, ImageFormat.Png);
            memoryStream.Seek (0, SeekOrigin.Begin);
            
            // Decode the icon
            PngBitmapDecoder bitmapDecoder = new PngBitmapDecoder (memoryStream, BitmapCreateOptions.None, BitmapCacheOption.Default);
            if (bitmapDecoder == null || bitmapDecoder.Frames == null || bitmapDecoder.Frames.Count == 0)
            {
                throw new Exception ("Failed to decode icon.");
            }

            // Retrn the icon
            return bitmapDecoder.Frames[0];
        }
        #endregion

        #region Native Constants
        private const string IID_IImageList = "46EB5926-582E-4017-9FDF-E8998DAA0950";

        internal enum SHIL : uint
        {
            LARGE,
            SMALL,
            EXTRALARGE,
            SYSSMALL,
            JUMBO,
            LAST
        }

        [Flags]
        private enum SHGFI : uint
        {
            ICON =              0x000000100,
            DISPLAYNAME =       0x000000200,
            TYPENAME =          0x000000400,
            ATTRIBUTES =        0x000000800,
            ICONLOCATION =      0x000001000,
            EXETYPE =           0x000002000,
            SYSICONINDEX =      0x000004000,
            LINKOVERLAY =       0x000008000,
            SELECTED =          0x000010000,
            ATTR_SPECIFIED =    0x000020000,
            LARGEICON =         0x000000000,
            SMALLICON =         0x000000001,
            OPENICON =          0x000000002,
            SHELLICONSIZE =     0x000000004,
            PIDL =              0x000000008,
            USEFILEATTRIBUTES = 0x000000010,
            ADDOVERLAYS =       0x000000020,
            OVERLAYINDEX =      0x000000040
        }

        [Flags]
        private enum ILD : uint
        {
            NORMAL =        0x00000000,
            TRANSPARENT =   0x00000001,
            BLEND25 =       0x00000002,
            SELECTED =      0x00000004,
            MASK =          0x00000010,
            IMAGE =         0x00000020,
            ROP =           0x00000040,
            OVERLAYMASK =   0x00000F00,
            PRESERVEALPHA = 0x00001000,
            SCALE =         0x00002000,
            DPISCALE =      0x00004000
        }
        #endregion

        #region Native Structures
        [StructLayout (LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public int dwAttributes;
            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
        #endregion

        #region Native Functions
        [DllImport ("shell32")]
        private static extern IntPtr SHGetFileInfo (
            string pszPath,
            int dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport ("shell32")]
        private extern static int SHGetImageList (
            int iImageList,
            ref Guid riid,
            ref IntPtr handle);

        [DllImport ("comctl32")]
        private extern static IntPtr ImageList_GetIcon (
            IntPtr himl,
            int i,
            uint flags);
        #endregion
    }
}
