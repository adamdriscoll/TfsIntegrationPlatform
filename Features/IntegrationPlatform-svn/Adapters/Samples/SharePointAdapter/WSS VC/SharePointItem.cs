//------------------------------------------------------------------------------
// <copyright file="SharePointFile.cs" company="Microsoft Corporation">
//      Copyright © Microsoft Corporation.  All Rights Reserved.
// </copyright>
//
//  This code released under the terms of the 
//  Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//------------------------------------------------------------------------------

namespace Microsoft.TeamFoundation.Integration.SharePointVCAdapter
{
    using System;
    using System.IO;
    using System.Net;
    using Microsoft.TeamFoundation.Migration.Toolkit;

    /// <summary>
    /// Defines the type in SharePoint
    /// </summary>
    public enum SharePointItemType
    {
        /// <summary>
        /// File
        /// </summary>        
        File = 0,

        /// <summary>
        /// Directory
        /// </summary>
        Directory = 1
    }

    /// <summary>
    /// This class provides an object to represent a list item within SharePoint.
    /// </summary>
    [Serializable]
    public sealed class SharePointItem : IMigrationItem
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>The filename.</value>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the modified.
        /// </summary>
        /// <value>The modified.</value>
        public DateTime Modified { get; set; }

        /// <summary>
        /// Gets or sets the created.
        /// </summary>
        /// <value>The created.</value>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the absolute URL.
        /// </summary>
        /// <value>The absolute URL.</value>
        public string AbsoluteURL { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the type of the item.
        /// </summary>
        /// <value>The type of the item.</value>
        public SharePointItemType ItemType { get; set; }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        /// <value>The credentials.</value>
        public NetworkCredential Credentials { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharePointItem"/> class.
        /// </summary>
        public SharePointItem() 
        {
        }        

        #region IMigrationItem Members

        /// <summary>
        /// A display name for the item.  This string is not gaurenteed to useful for parsing
        /// or to represent a meaningful path within the version control system or local file system.
        /// </summary>
        /// <value></value>
        string IMigrationItem.DisplayName
        {
            get { return Filename; }
        }

        /// <summary>
        /// Downloads the item from the source system to the provided path.
        /// </summary>
        /// <param name="localPath">The path to download the item to.</param>
        void IMigrationItem.Download(string localPath)
        {
            TraceManager.TraceInformation("WSSVC:Item:Download:From {0} to {1}", this.AbsoluteURL, localPath);
            if (this.ItemType == SharePointItemType.File)
            {
                TraceManager.TraceInformation("\tType is file");
                string targetDir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(targetDir))
                {
                    TraceManager.TraceInformation("\tCreating Directory for file - {0}", targetDir);
                    Directory.CreateDirectory(targetDir);
                }

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(this.AbsoluteURL);
                webRequest.Credentials = this.Credentials;
                using (Stream responseStream = webRequest.GetResponse().GetResponseStream())
                {
                    using (FileStream fileStream = new FileStream(localPath, FileMode.CreateNew, FileAccess.ReadWrite))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        do
                        {
                            // Read data (up to 1k) from the stream
                            bytesRead = responseStream.Read(buffer, 0, buffer.Length);

                            // Write the data to the local file
                            fileStream.Write(buffer, 0, bytesRead);
                        } while (bytesRead > 0);
                    }
                }

                TraceManager.TraceInformation("\tFile downloaded successfully");
            }

            if (this.ItemType == SharePointItemType.Directory)
            {
                TraceManager.TraceInformation("\tType is directory");
                if (!Directory.Exists(localPath))
                {
                    TraceManager.TraceInformation("\tCreating Directory - {0}", localPath);
                    Directory.CreateDirectory(localPath);
                }
            }
        }

        #endregion
    }
}
