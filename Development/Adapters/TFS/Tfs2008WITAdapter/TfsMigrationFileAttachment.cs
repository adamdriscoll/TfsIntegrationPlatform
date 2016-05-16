// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.TeamFoundation.Migration.Tfs2008WitAdapter
{
    /// <summary>
    /// TFS file attachment.
    /// </summary>
    [Serializable]
    public class TfsMigrationFileAttachment : IMigrationFileAttachment, IMigrationItem
    {      
        /// <summary>
        /// The default constructor required by serialization
        /// </summary>
        public TfsMigrationFileAttachment()
        {
        }

        /// <summary>
        /// Consturctor.  Initializes file attachment.
        /// </summary>
        /// <param name="attach">Attachment information from WIT OM</param>
        public TfsMigrationFileAttachment(Attachment attach)
        {
            m_attachment = attach;
            m_fileId = null;

            m_name = m_attachment.Name;
            m_length = m_attachment.Length;
            m_utcCreationDate = m_attachment.CreationTimeUtc;
            m_utcLastWriteDate = m_attachment.LastWriteTimeUtc;
            m_comment = m_attachment.Comment;
            m_uri = m_attachment.Uri;
            m_absoluteUri = m_uri.AbsoluteUri;
        }

        internal TfsMigrationFileAttachment(FileAttachmentMetadata attach)
        {
            m_attachment = null;
            m_fileId = null;
            m_name = attach.Name;
            m_length = attach.Length;
            m_utcCreationDate = attach.UtcCreationDate;
            m_utcLastWriteDate = attach.UtcLastWriteDate;
            m_comment = attach.Comment;
            m_uri = null;
            m_absoluteUri = null;
        }

        /// <summary>
        /// Returns the file ID.
        /// </summary>
        public int FileID 
        { 
            get 
            {
                if (m_fileId.HasValue)
                {
                    return m_fileId.Value;
                }

                if (null == m_attachment)
                {
                    return -1;
                }

                return GetFileID(m_attachment.Uri.Query); 
            }
            set
            {
                m_fileId = value;
            }
        }

        /// <summary>
        /// Gets absolute Uri string of the attachment
        /// </summary>
        public string AbsoluteUri
        {
            get { return m_absoluteUri; }
            set { m_absoluteUri = value; }
        }

        #region IMigrationFileAttachment Members
        /// <summary>
        /// Returns the name of the file.
        /// </summary>
        public string Name 
        { 
            get 
            { 
                return m_name; 
            }
            set
            {
                m_name = value;
            }
        }

        /// <summary>
        /// Returns the file size.
        /// </summary>
        public long Length 
        { 
            get 
            { 
                return m_length; 
            }
            set
            {
                m_length = value;
            }
        }

        /// <summary>
        /// Returns the date/time the file was created.
        /// </summary>
        public DateTime UtcCreationDate 
        { 
            get 
            { 
                return m_utcCreationDate; 
            }
            set
            {
                m_utcCreationDate = value;
            }
        }

        /// <summary>
        /// Returns the date/time the file was last updated.
        /// </summary>
        public DateTime UtcLastWriteDate 
        { 
            get 
            { 
                return m_utcLastWriteDate; 
            }
            set
            {
                m_utcLastWriteDate = value;
            }
        }

        /// <summary>
        /// Returns any comment about the file.
        /// </summary>
        public string Comment 
        { 
            get 
            { 
                return m_comment; 
            }
            set
            {
                m_comment = value;
            }
        }

        /// <summary>
        /// Gets the contents of the file when needed for comparison.
        /// </summary>
        /// <returns>Contents of the file.</returns>
        public Stream GetFileContents()
        {
            if (string.IsNullOrEmpty(this.m_localDir))
            {
                throw new InvalidOperationException();
            }

            // Download attachment into the given file
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(AbsoluteUri));
            request.Method = "GET";
            request.Credentials = CredentialCache.DefaultNetworkCredentials;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new WitMigrationException(string.Format(CultureInfo.InvariantCulture, 
                        TfsWITAdapterResources.BadHttpResponse, response.StatusCode.ToString()));
                }

                // Copy result to localFile stream
                using (Stream responseStream = response.GetResponseStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    // Create the new file then stream into it
                    FileStream localFile = new FileStream(
                        m_localDir,
                        FileMode.CreateNew, 
                        FileAccess.ReadWrite, 
                        FileShare.None, 
                        buffer.Length, 
                        FileOptions.SequentialScan);
                    try
                    {
                        while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            localFile.Write(buffer, 0, bytesRead);
                        }

                        // Rewind
                        localFile.Seek(0, SeekOrigin.Begin);
                        return localFile;
                    }
                    catch
                    {
                        localFile.Dispose();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region IMigrationItem Members

        public void Download(string localPath)
        {
            this.m_localDir = localPath;
            this.GetFileContents().Close();
        }

        public string DisplayName
        {
            get 
            {
                return string.Format(TfsWITAdapterResources.TfsAttachmentDisplayName, this.Name, this.FileID);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the file ID from the query string.
        /// </summary>
        /// <param name="qstring">Query string from the attachment Uri.</param>
        /// <returns>File ID.</returns>
        private int GetFileID(string qstring)
        {
            if (m_fileId == null)
            {
                NameValueCollection qargs = HttpUtility.ParseQueryString(qstring);
                string fileIdStr = qargs.Get("FileID");
                if (string.IsNullOrEmpty(fileIdStr) == false)
                {
                    try
                    {
                        m_fileId = int.Parse(fileIdStr, CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(TfsWITAdapterResources.FileIdNotInteger, ex);
                    }
                }
                if (m_fileId <= 0)
                {
                    m_fileId = null;
                    throw new ArgumentException(TfsWITAdapterResources.FileIdNotFound);
                }
            }
            return (int)m_fileId;
        }

        #endregion


        private Attachment m_attachment;        // WIT OM object representing a file attachment
        private Nullable<int> m_fileId;         // ID of the file attachment
        private string m_localDir = string.Empty;

        private string m_name;
        private long m_length;
        private DateTime m_utcCreationDate;
        private DateTime m_utcLastWriteDate;
        private string m_comment;
        private Uri m_uri;
        private string m_absoluteUri;
    }
}
