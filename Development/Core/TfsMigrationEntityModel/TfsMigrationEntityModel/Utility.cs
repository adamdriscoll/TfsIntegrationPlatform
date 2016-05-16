// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Microsoft.TeamFoundation.Migration
{
    public static class Utility
    {
        internal static string GetUniqueIdString()
        {
            return Guid.NewGuid().ToString().ToLower();
        }

		/// <summary>
		/// Gets the property descriptor for the specified object and property name.
		/// </summary>
		/// <remarks>
		/// If the specified property does not exist in the specified object,
		/// an ArgumentException is thrown.
		/// </remarks>
		/// <param name="obj">The object to which the specified property belongs.</param>
		/// <param name="propertyName">The property for which to obtain a property descriptor.</param>
		/// <returns>The property descriptor.</returns>
		public static PropertyDescriptor GetPropertyDescriptor (object obj, string propertyName)
		{
			PropertyDescriptor propertyDescriptor = TryGetPropertyDescriptor (obj, propertyName);
			if (propertyDescriptor == null)
			{
				throw new ArgumentException (string.Format ("{0} is not a property of {1}", propertyName, obj.GetType ().Name));
			}
			return propertyDescriptor;
		}

		/// <summary>
		/// Gets the property descriptor for the specified object and property name.
		/// </summary>
		/// <remarks>
		/// If the specified property does not exist in the specified object,
		/// a null reference is returned.
		/// </remarks>
		/// <param name="obj">The object to which the specified property belongs.</param>
		/// <param name="propertyName">The property for which to obtain a property descriptor.</param>
		/// <returns>The property descriptor.</returns>
		public static PropertyDescriptor TryGetPropertyDescriptor (object obj, string propertyName)
		{
			foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties (obj))
			{
				if (propertyDescriptor.Name == propertyName)
				{
					return propertyDescriptor;
				}
			}

			return null;
		}

        public static string ReplaceGuids(string content, Dictionary<string, string> guidMappings)
        {
            foreach (var v in guidMappings)
            {
                content = Regex.Replace(content, v.Key, v.Value, RegexOptions.IgnoreCase);
            }

            return content;
        }

        public static Dictionary<string, string> CreateGuidStringMappings(string content, bool reGuid)
        {
            Dictionary<string, string> guidMappings = new Dictionary<string, string>();
            string guidPattern = "[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}";
            foreach (var match in Regex.Matches(content, guidPattern))
            {
                if (!guidMappings.ContainsKey(match.ToString()))
                {
                    if (reGuid)
                    {
                        guidMappings[match.ToString()] = Guid.NewGuid().ToString().ToLowerInvariant();
                    }
                    else
                    {
                        guidMappings[match.ToString()] = match.ToString().ToLowerInvariant();
                    }
                }
            }
            return guidMappings;
        }

        #region File operations

        public static byte[] CalculateMD5Hash(string fileName)
        {
            byte[] hash;

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open,
                                                          FileAccess.Read, FileShare.Read))
            {
                hash = CalculateMD5(fileStream);
            }

            return hash;
        }

        public static byte[] CalculateMD5(Stream stream)
        {
            byte[] hash;

            using (MD5 md5Provider = new MD5CryptoServiceProvider())
            {
                hash = md5Provider.ComputeHash(stream);
            }

            return hash;
        }
        #endregion
	}
}
