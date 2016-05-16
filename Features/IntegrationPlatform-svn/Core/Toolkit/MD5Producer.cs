// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class MD5Producer
    {
        bool m_md5ProviderDisabled = false;

        //********************************************************************************************
        /// <summary>
        /// Gets the flag of whether the MD5 provider is disabled or not
        /// </summary>
        //********************************************************************************************
        public bool Md5ProviderDisabled
        {
            get { return m_md5ProviderDisabled; }
            private set { m_md5ProviderDisabled = value; }
        }

        //********************************************************************************************
        /// <summary>
        /// Computes the MD5 hash for the specified file, unless FIPS enforcement is enabled.
        /// </summary>
        /// <param name="fileName">file to hash</param>
        /// <returns>the MD5 hash if the provider can be created or a zero-length array if that fails
        /// </returns>
        //********************************************************************************************
        public byte[] CalculateMD5(string fileName)
        {
            byte[] hash;

            // If we know we can't calculate the hash, there's no point in opening the file.
            if (Md5ProviderDisabled)
            {
                hash = new byte[0];
            }
            else
            {
                using (FileStream fileStream = new FileStream(fileName, FileMode.Open,
                                                              FileAccess.Read, FileShare.Read))
                {
                    hash = CalculateMD5(fileStream);
                }
            }

            return hash;
        }

        //********************************************************************************************
        /// <summary>
        /// Computes the MD5 hash for the specified stream, unless FIPS enforcement is enabled.
        /// </summary>
        /// <param name="stream">the stream to hash</param>
        /// <returns>the MD5 hash if the provider can be created or a zero-length array if that fails
        /// </returns>
        //********************************************************************************************
        public byte[] CalculateMD5(Stream stream)
        {
            byte[] hash;

            MD5 md5Provider = GetMD5Provider();
            if (md5Provider == null)
            {
                hash = new byte[0];
            }
            else
            {
                using (md5Provider)
                {
                    hash = md5Provider.ComputeHash(stream);
                }
            }

            return hash;
        }

        //********************************************************************************************
        /// <summary>
        /// Compares two MD5 hash
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        //********************************************************************************************
        public int CompareMD5(byte[] x, byte[] y)
        {
            if (x.Length == 0)
            {
                throw new ArgumentException(MigrationToolkitResources.InvalidMD5HashCode, "x");
            }

            if (y.Length == 0)
            {
                throw new ArgumentException(MigrationToolkitResources.InvalidMD5HashCode, "y");
            }

            int compareRslt = 0;
            compareRslt = x.Length.CompareTo(y.Length);
            if (compareRslt != 0)
            {
                return compareRslt;
            }

            for (int index = 0; index < x.Length; ++index)
            {
                compareRslt = x[index].CompareTo(y[index]);
                if (compareRslt != 0)
                {
                    return compareRslt;
                }
            }

            return compareRslt;
        }

        //********************************************************************************************
        /// <summary>
        /// This helper method takes care of determining whether we can use MD5, which throws when
        /// the FIPS enforcement is turned on in Windows.
        /// </summary>
        /// <returns>the MD5 provider or null if FIPS prevents using it</returns>
        //********************************************************************************************
        private MD5 GetMD5Provider()
        {
            MD5 md5Provider = null;
            if (!Md5ProviderDisabled)
            {
                try
                {
                    md5Provider = new MD5CryptoServiceProvider();
                }
                catch (InvalidOperationException e)
                {
                    Md5ProviderDisabled = true;
                    TraceManager.TraceError("MD5CryptoProvider is unavailable (hash is now disabled): " + e);
                }
            }

            return md5Provider;
        }
    }
}
