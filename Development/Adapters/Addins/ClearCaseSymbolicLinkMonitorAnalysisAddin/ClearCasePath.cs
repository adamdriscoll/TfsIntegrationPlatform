// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ClearCaseSymbolicLinkMonitorAnalysisAddin
{
    // TODO: Reference the ClearCasePath class in the CC adapter instead of copying the class
    internal static class ClearCasePath
    {
        internal const char VobRoot = '.';
        internal const string ExtendedNamingSuffix = "@@";
        internal const char Separator = '\\';
        internal const char UnixSeparator = '/';
        internal const string MainBranchName = "main";
        internal const char DriveSeperator = '"';
        internal const string LostFoundFolder = "lost+found";

        /// <summary>
        /// Compare two file specs for equality.
        /// </summary>
        /// <param name="item1">First item to compare.</param>
        /// <param name="item2">Second item to compare.</param>
        /// <returns>Return true if the items match.</returns>
        internal static bool Equals(String item1, String item2)
        {
            if ((Object)item1 == (Object)item2)
            {
                return true;
            }

            return String.Equals(item1, item2, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetFullPath(string item)
        {
            return GetFullPath(item, false);
        }

        internal static string MakeRelative(string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                throw new ArgumentNullException("item");
            }

            if ((item[0] == Separator) || (item[0] == UnixSeparator))
            {
                return GetFullPath(item).Substring(1);
            }
            return item;
        }

        internal static string removeViewLocationFromVersion(string versionExtendedPath, string viewLocation)
        {
            if (string.IsNullOrEmpty(versionExtendedPath))
            {
                return null;
            }
            if (string.IsNullOrEmpty(viewLocation))
            {
                return null;
            }

            Debug.Assert(versionExtendedPath.StartsWith(viewLocation), "The versionExtendedPath is not started with a view location ");

            return versionExtendedPath.Substring(viewLocation.Length);
        }

        internal static string GetAbsoluteVobPathFromVersionExtendedPath(string versionExtendedPath)
        {
            if (string.IsNullOrEmpty(versionExtendedPath))
            {
                return versionExtendedPath;
            }

            int extendedNamingSuffixIndex = versionExtendedPath.LastIndexOf(ExtendedNamingSuffix);
            if (extendedNamingSuffixIndex > 0)
            {
                return versionExtendedPath.Substring(0, extendedNamingSuffixIndex);
            }
            else
            {
                return versionExtendedPath;
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Return true if the item is equal to or is under (in the heirarchy) parent.
        /// </summary>
        /// <param name="item">Item to check.</param>
        /// <param name="parent">Parent to check against.</param>
        /// <returns>true if item is equal or under parent.</returns>
        //********************************************************************************************
        internal static bool IsSubItem(String item, String parent)
        {
            return IsSubItem(item, parent, true);
        }

        /// <summary>
        /// Return true if the item is equal to or is under (in the heirarchy) parent.
        /// </summary>
        /// <param name="item">Item to check.</param>
        /// <param name="parent">Parent to check against.</param>
        /// <param name="isServerPath">True if the paths are server paths.</param>
        /// <returns>true if item is equal or under parent.</returns>
        internal static bool IsSubItem(string item, String parent, bool isServerPath)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            Debug.Assert((!isServerPath) || (item == GetFullPath(item)), "item must be canonicalized");
            Debug.Assert((!isServerPath) || (parent == GetFullPath(parent)), "folder must be canonicalized");

            return item.StartsWith(parent, StringComparison.OrdinalIgnoreCase) &&
                   (item.Length == parent.Length || parent[parent.Length - 1] == Separator || item[parent.Length] == Separator);
        }
        /// <summary>
        /// Given an item and its parent, return the path relative to its parent.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        internal static string MakeRelative(string item, string parent)
        {
            if (!IsSubItem(item, parent))
            {
                return item;
            }

            return MakeRelative(item.Substring(parent.Length));
        }

        //********************************************************************************************
        /// <summary>
        /// Canonicalize the specified items specification and return the string.  This will do things
        /// like remove convert / to \, etc.  It throws an InvalidPathException if the item
        /// is the empty string, contains invalid characters including an embedded dollar sign
        /// (wildcards are not considered illegal in this check), or is too long.
        /// </summary>
        /// <param name="item">Item specification to canonicalize</param>
        /// <param name="checkReservedCharacters">boolean flag whether to validate 
        /// version control reserved characters or not.</param>
        /// <returns>Canonical string form of the item.</returns>
        //********************************************************************************************
        internal static string GetFullPath(string item, bool checkReservedCharacters)
        {
            if (string.IsNullOrEmpty(item))
            {
                return item;
            }
            int itemLength = item.Length;

            if (item[0] != Separator && item[0] != UnixSeparator)
            {
                throw new ArgumentException("item");
            }
            return item.Replace(UnixSeparator, Separator);
        }

        /// <summary>
        /// Combine a path and version into a clear case versioned item.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        internal static string CombinePathWithVersion(string path, string version)
        {
            if (path == null)
            {
                throw new ArgumentNullException("extendedPath");
            }
            if (version == null)
            {
                throw new ArgumentNullException("versionPath");
            }

            // To do standardize the paths

            return path + ExtendedNamingSuffix + version;
        }

        internal static bool IsVobRoot(string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                return false;
            }
            if (item[item.Length - 1] == VobRoot)
            {
                // \vob_name\.
                return true;
            }
            if (item.LastIndexOf(Separator) == 0)
            {
                // \vob_name
                return true;
            }
            return false;
        }

        //********************************************************************************************
        /// <summary>
        /// Return the last path component from an element path.  For example, passing "\foo_vob\bar"
        /// would return "bar".  Passing "\" will return "".
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        //********************************************************************************************
        internal static String GetFileName(String item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            // Get the index of the right most slash.
            int index = item.LastIndexOf(Separator);
            if (index < 0)
            {
                throw new ArgumentException("item");
            }

            // Return "" if this is the root.
            if (index == item.Length - 1)
            {
                return String.Empty;
            }

            // Return the part after the last slash.
            return item.Substring(index + 1);
        }

        //********************************************************************************************
        /// <summary>
        /// Given the last part of a version extended path, return the branch name. 
        /// E.g. \foo_vob\bar@@\main\2 will return \main as the branch
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        //********************************************************************************************
        internal static string GetBranchName(string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                return null;
            }
            if (item.LastIndexOf(Separator) < 0)
            {
                return item;
            }
            else
            {
                return item.Remove(item.LastIndexOf(Separator));
            }
        }

        //********************************************************************************************
        /// <summary>
        ///  Returns the number of levels of path elements in this spec up to the maximum depth.
        ///  \ will return 0
        ///  \foo will return 1
        ///  \foo\bar will return 2, and so on...
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        //********************************************************************************************
        public static int GetFolderDepth(String item)
        {
            int count = 0;

            if (!ClearCasePath.Equals(item, Separator))
            {
                // Count the slashes
                for (int lastIndex = item.IndexOf(Separator); (lastIndex != -1); lastIndex = item.IndexOf(Separator, lastIndex + 1))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Return true if the item is an element path.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal static bool IsElementPath(string item)
        {
            // Todo
            return true;
        }

        //********************************************************************************************
        /// <summary>
        /// Get the parent folder for the specified element path.  If "\\vob_name\." is passed, "\\vob_name\." is
        /// returned as the parent.
        /// </summary>
        /// <param name="item">Path to get the parent folder for.</param>
        /// <returns>The parent folder.</returns>
        //********************************************************************************************
        internal static String GetFolderName(String item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            Debug.Assert(IsElementPath(item), "The item must be an element path.");

            if (IsVobRoot(item))
            {
                return item;
            }
            // Get the index of the right most slash.
            int index = item.LastIndexOf(Separator);
            if (index < 0)
            {
                throw new ArgumentException("item");
            }

            // Return upto but not including the last slash.
            return item.Substring(0, index);
        }

        //********************************************************************************************
        /// <summary>
        /// Return a path constructed from parent and relative.  
        /// </summary>
        /// <param name="parent">The parent path to append relative to.</param>
        /// <param name="relative">The potentially partial path to qualify relative to parent.</param>
        /// <returns>The combined path.</returns>
        //********************************************************************************************
        internal static string Combine(string parent, string relative)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            if (relative == null)
            {
                throw new ArgumentNullException("relative");
            }

            string item;

            // If the relative path is the empty string or just "\", use the parent.
            // This makes some of the parsing below easier.
            if (relative.Length == 0 ||
                (relative.Length == 1 && relative[0] == Separator))
            {
                item = parent;
            }
            else
            {
                // Remove the '.' for vob root path.
                if (parent.Length > 0 && parent[parent.Length - 1] == VobRoot)
                {
                    parent = parent.Remove(parent.Length - 2);
                }
                // Figure out if we need a separator or if the parent already has one.
                if (parent.Length > 0 && parent[parent.Length - 1] != Separator)
                {
                    if (relative[0] == Separator)
                    {
                        item = parent + relative;
                    }
                    else
                    {
                        item = parent + Separator + relative;
                    }
                }
                else
                {
                    if (relative[0] == Separator)
                    {
                        item = parent + relative.Substring(1);
                    }
                    else
                    {
                        item = parent + relative;
                    }
                }
            }

            return item;
        }

        internal static string GetViewExtendedPath(string path, string vobPath, string viewName, string storageLocation)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(vobPath)
                || string.IsNullOrEmpty(viewName) || string.IsNullOrEmpty(storageLocation))
            {
                return null;
            }

            return storageLocation + Separator + viewName + vobPath + Separator + path;
        }

        /// <summary>
        /// Gets the vob name from vobpath
        /// </summary>
        /// <param name="dataSourcePath">Path starting with VOB name</param>
        /// <returns>Vob name</returns>
        internal static string GetVobName(string vobPath)
        {
            if (string.IsNullOrEmpty(vobPath))
            {
                throw new ArgumentNullException("vobPath");
            }

            string[] splitter = vobPath.Split(Separator);

            if (splitter.Length == 1)
            {
                //TODO Replace this exception. 
                //Handle it wherever this function is used
                throw new InvalidOperationException();
            }

            // Todo, change this to a combine.
            return Separator + splitter[1];
        }

        /// <summary>
        /// Combine two paths into one Vob Path
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="parentVobPath"></param>
        /// <returns></returns>
        internal static string CombineVobPath(string relativePath, string parentVobPath)
        {
            Debug.Assert(ValidateAbsoluteVobPath(parentVobPath), "Error - parentVobPath is not a valid absolute Vob path");
            if (string.IsNullOrEmpty(relativePath))
            {
                return parentVobPath;
            }
            Debug.Assert(relativePath[0] == Separator, "Error - relative path cannot start with a '\\'");
            return (parentVobPath + Separator + relativePath);
        }

        internal static string CombineVersionExtendedPath(string elementPath, string branchAbsolutePath, string versionNumber)
        {
            Debug.Assert(ValidateAbsoluteVobPath(branchAbsolutePath), "Error - parentVobPath is not a valid absolute Vob path");

            string versionSelector = branchAbsolutePath + Separator + versionNumber;
            string versionExtendedPath = elementPath + ExtendedNamingSuffix + versionNumber;
            return versionExtendedPath;
        }

        /// <summary>
        /// Verify the input Vob absolute path string.
        /// 1. The first character must be '\'
        /// 2. The last character cannot be '\'
        /// </summary>
        /// <param name="vobPath"></param>
        /// <returns></returns>
        internal static bool ValidateAbsoluteVobPath(string vobPath)
        {
            if (string.IsNullOrEmpty(vobPath))
            {
                return false;
            }
            if (vobPath[0] != Separator)
            {
                return false;
            }
            if (vobPath[vobPath.Length - 1] == Separator)
            {
                return false;
            }
            //Todo verify invalid characters.
            return true;
        }
    }
}
