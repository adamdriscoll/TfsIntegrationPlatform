// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace Microsoft.TeamFoundation.Migration.TfsFileSystemAdapter
{
    class Utils
    {
        /// <summary>
        /// If the parent directory to the specified file does not exist the parent directory is created.
        /// </summary>
        /// <param name="path">The file whose parent directory should be created.</param>
        internal static void EnsurePathToFileExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            string directoryName = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directoryName);
        }

        /// <summary>
        ///  Compare the hash values for two files in the tfs servers
        /// </summary>
        /// <param name="filePath">The file on local file system.</param>
        /// <param name="sourceMd5Sum">The other files hash value</param>
        /// <returns>true if the hash values are the same for both files</returns>
        internal static bool ContentsMatch(string filePath, byte[] targetMd5Sum)
        {
            byte[] sourceMd5Sum;

            try
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open,
                                                              FileAccess.Read, FileShare.Read))
                {
                    using (MD5 md5Provider = new MD5CryptoServiceProvider())
                    {
                        sourceMd5Sum = md5Provider.ComputeHash(fileStream);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            if (targetMd5Sum.Length != sourceMd5Sum.Length)
            {
                return false;
            }

            for (int i = 0; i < targetMd5Sum.Length; i++)
            {
                if (targetMd5Sum[i] != sourceMd5Sum[i])
                {
                    return false;
                }
            }
            return true;
        }

        // The ContentsMatch function below was copied from: http://support.microsoft.com/kb/320348 and modified
        // slightly to use usings to make sure the files are closed properly.

        // This method accepts two strings the represent two files to 
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the 
        // files are not the same.
        internal static Boolean ContentsMatch(string localFile1, string localFile2)
        {
            int file1byte;
            int file2byte;

            // Determine if the same file was referenced two times.
            if (localFile1 == localFile2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            using (FileStream fs1 = new FileStream(localFile1, FileMode.Open))
            using (FileStream fs2 = new FileStream(localFile2, FileMode.Open))
            {
                // Check the file sizes. If they are not the same, the files 
                // are not the same.
                if (fs1.Length != fs2.Length)
                {
                    // Return false to indicate files are different
                    return false;
                }

                // Read and compare a byte from each file until either a
                // non-matching set of bytes is found or until the end of
                // file1 is reached.
                do
                {
                    // Read one byte from each file.
                    file1byte = fs1.ReadByte();
                    file2byte = fs2.ReadByte();
                }
                while ((file1byte == file2byte) && (file1byte != -1));

                // Return the success of the comparison. "file1byte" is 
                // equal to "file2byte" at this point only if the files are 
                // the same.
                return ((file1byte - file2byte) == 0);
            }
        }

        /// <summary>
        /// Deletes the specified file.  Removes the ReadOnly attribute if it is set on the
        /// file.
        /// </summary>
        /// <param name="localPath">The local file to delete</param>
        internal static void DeleteFile(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
            {
                throw new ArgumentNullException("localPath");
            }

            if (File.Exists(localPath))
            {
                FileAttributes attr = File.GetAttributes(localPath);
                if ((attr & FileAttributes.ReadOnly) != 0)
                {
                    attr &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(localPath, attr);
                }

                File.Delete(localPath);
            }
        }
    }
}
