// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ClearCase;

namespace Microsoft.TeamFoundation.Migration.ClearCaseDetailedHistoryAdapter
{
    /// <summary>
    /// This class represnet a versioned item on clearcase. 
    /// It wraps the item path, branch and version selector.
    /// </summary>
    public class CCItem
    {
        public string VersionExtendedPath { get; private set; }
        public string AbsoluteVobPath { get; private set; }
        public string AbsoluteVobPathAtTheVersion { get; private set; }
        public string Branch { get; private set; }
        public ItemType ItemType { get; private set; }

        /// <summary>
        /// Constructor. 
        /// Construct the clearcase item from a version object on ClearCase
        /// </summary>
        /// <param name="version"></param>
        public CCItem(CCVersion version, string vobName)
        {
            ItemType = ItemType.Version;
            VersionExtendedPath = version.ExtendedPath;
            AbsoluteVobPath = vobName;

            string[] splittedPath = VersionExtendedPath.Split(new string[] { ClearCasePath.ExtendedNamingSuffix }, StringSplitOptions.None);
            Branch = ClearCasePath.GetBranchName(splittedPath[splittedPath.Length - 1]);

            List<string> parentPaths = new List<string>();

            while (! ClearCasePath.IsVobRoot(version.Element.Path))
            {
                parentPaths.Add(ClearCasePath.GetFileName(version.Element.Path));
                try
                {
                    version = version.Parent;
                }
                catch (COMException)
                {
                    // This is a ClearCase CAL bug. 
                    // Ignore the root node. 
                    parentPaths.RemoveAt(parentPaths.Count - 1);
                    break;
                }
            }
            parentPaths.Reverse();

            foreach (string parentPath in parentPaths)
            {
                AbsoluteVobPath = ClearCasePath.Combine(AbsoluteVobPath, parentPath);
            }
            AbsoluteVobPath = AbsoluteVobPath;
            AbsoluteVobPathAtTheVersion = AbsoluteVobPath;
        }


        /// <summary>
        /// Constructor. 
        /// Construct the clearcase item from a standard vob extended path.
        /// </summary>
        /// <param name="VersionExtendedPath">The extended vob path in the format /vob_name/folder_name/file_name@@version_selector</param>
        /// <param name="itemType"></param>
        /// <param name="vobName"></param>
        public CCItem(string versionExtendedPath, ItemType itemType, string vobName)
        {
            string[] splittedPath = versionExtendedPath.Split(new string[] { ClearCasePath.ExtendedNamingSuffix }, StringSplitOptions.None);
            if (splittedPath.Length < 2)
            {
                throw new InvalidPathException(string.Format(CCResources.InvalidVobExtendedPath, versionExtendedPath));
            }

            AbsoluteVobPath = splittedPath[0];
            int vobNameIndex = AbsoluteVobPath.IndexOf(vobName);

            if (vobNameIndex < 0)
            {
                throw new InvalidPathException(string.Format(CCResources.NoVobNameInVobExtendedPath, versionExtendedPath, vobName));
            }
            else
            {
                AbsoluteVobPath = AbsoluteVobPath.Substring(vobNameIndex);
            }

            if (AbsoluteVobPath[AbsoluteVobPath.Length - 1] == ClearCasePath.VobRoot)
            {
                AbsoluteVobPath = AbsoluteVobPath.Remove(AbsoluteVobPath.Length - 2); // also remove the \ before the .
            }

            string lastPartOfVersionExtendedPath = splittedPath[splittedPath.Length - 1];
            switch (itemType)
            {
                case ItemType.Element:
                    if (lastPartOfVersionExtendedPath != string.Empty)
                    {
                        throw new InvalidPathException(string.Format(CCResources.InvalidVobExtendedPathForElement, versionExtendedPath));
                    }
                    ItemType = ItemType.Element;
                    Branch = null;
                    VersionExtendedPath = versionExtendedPath;
                    return;
                case ItemType.Branch:
                    if (lastPartOfVersionExtendedPath == string.Empty)
                    {
                        throw new InvalidPathException(string.Format(CCResources.InvalidVobExtendedPathForBranch, versionExtendedPath));
                    }
                    ItemType = ItemType.Branch;
                    Branch = lastPartOfVersionExtendedPath;
                    VersionExtendedPath = versionExtendedPath;
                    return;
                case ItemType.Version:
                    if (splittedPath[1] == string.Empty)
                    {
                        throw new InvalidPathException(string.Format(CCResources.InvalidVobExtendedPathForVersion, versionExtendedPath));
                    }
                    // The last part is the branch of the leaf item
                    Branch = lastPartOfVersionExtendedPath.Remove(lastPartOfVersionExtendedPath.LastIndexOf(ClearCasePath.Separator));
                    ItemType = ItemType.Version;
                    VersionExtendedPath = versionExtendedPath;
                    break;
                case ItemType.DerivedObject:
                    // Todo
                    throw new NotImplementedException("Don't know how to handle derived object");
                    break;
                default:
                    throw new NotImplementedException("Unknown item type");
                    break;
            }

            AbsoluteVobPathAtTheVersion = AbsoluteVobPath;
        }

        /// <summary>
        /// Compare two CCItem object by VobExtededePath and 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                CCItem otherItem = (CCItem)obj;
                if (otherItem == null)
                {
                    return false;
                }
                return ((otherItem.VersionExtendedPath == VersionExtendedPath)
                    && (otherItem.ItemType == ItemType));
            }
        }
    }

    /// <summary>
    /// Represent the type of ClearCase object.
    /// </summary>
    public enum ItemType
    {
        Element,
        Branch,
        Version,
        VobSimbolicLink,
        DerivedObject
    }
}
