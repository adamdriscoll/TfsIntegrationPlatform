using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Web;

namespace Microsoft.TeamFoundation.Migration.SubversionAdapter
{
    public static class PathUtils
    {
        public const char Separator = '/';

        /// <summary>
        /// Canonicalize the specified items specification and return the uri.  This will do things
        /// like remove ..'s, It throws an Exception if the item is the empty string, contains invalid characters
        /// </summary>
        /// <param name="item">Item specification to canonicalize</param>
        /// <returns>Canonical URI of the item.</returns>
        public static Uri GetNormalizedPath(string item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            //remove the trailing slashes. This fact has to be taken in consideration when you compare paths
            item = item.TrimEnd(Separator);

            //TODO check for invalid chars that are not allowed on windows file systems or any other check for invalid paths.

            //Converts the string to an item. Therefore the URI object will do all the validation for us
            return new Uri(item);
        }

        /// <summary>
        /// Return a fully qualified and canonicalized path constructed from parent and relative.
        /// Return the path that results from appending relative to parent and canonicalizing the result.  
        /// It is assumed that parent is already canonicalized.
        /// </summary>
        /// <param name="parent">The parent path to append relative to</param>
        /// <param name="relative">The potentially partial path to qualify relative to parent.</param>
        /// <returns>The combined, canonicalized path.</returns>
        internal static Uri Combine(Uri parent, string relative)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            if (relative == null)
            {
                throw new ArgumentNullException("relative");
            }

            //remove all trailing speperators from the parent path. there shouldn't be any leadings due to the uri format
            var normalizedParent = parent.AbsoluteUri.TrimEnd(Separator);

            //remove alle trailing and leading path seperators. this is needed to have an normalized string for the conact operation
            relative = relative.Trim(Separator);

            var combinedPath = string.Format("{0}{1}{2}", normalizedParent, Separator, relative);

            // canonicalizes the path and converts it to an URI object. Therefore we have an implicit validation wether our URI is valid
            return GetNormalizedPath(combinedPath);
        }

        /// <summary>
        /// Return a fully qualified and canonicalized path constructed from parent and relative.
        /// Return the path that results from appending relative to parent and canonicalizing the result.  
        /// It is assumed that parent is already canonicalized.
        /// </summary>
        /// <param name="parent">The parent path to append relative to</param>
        /// <param name="relative">The potentially partial path to qualify relative to parent.</param>
        /// <returns>The combined, canonicalized path.</returns>
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

            //remove all trailing speperators from the parent path. there shouldn't be any leadings due to the uri format
            var normalizedParent = parent.TrimEnd(Separator);

            //remove alle trailing and leading path seperators. this is needed to have an normalized string for the conact operation
            relative = relative.Trim(Separator);

            return string.Format("{0}{1}{2}", normalizedParent, Separator, relative);
        }

        /// <summary>
        /// Determine wether an item (child) actually is  a child item of the parent.
        /// </summary>
        /// <param name="parent">The parent path that is the reference for the operation</param>
        /// <param name="child">The child path </param>
        /// <returns>true if it is a child of the parent item; false otherwise</returns>
        internal static bool IsChildItem (Uri parent, Uri child)
        {
            if (null == parent)
            {
                throw new ArgumentNullException("parent");
            }

            if (null == child)
            {
                throw new ArgumentNullException("child");
            }

            //We are going to append a trailing slash to both paths. 
            //This is needed so that we can compare them with StartsWith.
            //Without the appendix we could get false results because the following example would return true
            // parent1: svn://localhost/repo/Release
            // parent2: svn://localhost/repo/Release-2.0
            // child: svn://localhost/repo/Release-2.0/File.txt
            // Both cases would return true. Adding an trailing slash prevents this
            var normalizedParent = AppendTrailingSlash(parent.AbsoluteUri);
            var normalizedChild = AppendTrailingSlash(child.AbsoluteUri);

            if (normalizedChild.Length < normalizedParent.Length)
            {
                return false;
            }

            //TODO carfully with capitalized paths. Capitalization is currenntly out of scope. Has to be addressed later
            return normalizedChild.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Determine wether an item (child) actually is  a child item of the parent.
        /// </summary>
        /// <param name="parent">The parent path that is the reference for the operation</param>
        /// <param name="child">The child path </param>
        /// <returns>true if it is a child of the parent item; false otherwise</returns>
        internal static bool IsChildItem(string parent, Uri child)
        {
            return IsChildItem(new Uri(parent), child);
        }

        internal static bool IsChildItem(string parent, string child)
        {
            return IsChildItem(new Uri(parent), new Uri(child));
        }


        /// <summary>
        /// Ensures that the path ends with a trailing slash
        /// </summary>
        /// <param name="path">The path that has to be processed</param>
        /// <returns>A string that ends with an trailing slash</returns>
        internal static string AppendTrailingSlash(string path)
        {
            if (null == path)
            {
                throw new ArgumentNullException("path");
            }

            if (!path.EndsWith(Separator.ToString()))
            {
                path = path + Separator;
            }

            return path;
        }

        /// <summary>
        /// This method will convert an fully qualified uri of a file to a folder to an relative uri that points
        /// to the same file folder relative based on the defined baseuri
        /// <para/>
        /// Example:
        /// <code>
        /// fullUri: svn://localhost/repos/svn2tofs/Folder/File.txt
        /// baseUri: svn://localhost/repos/svn2tfs/
        /// result: /Folder/File.txt
        /// </code>
        /// </summary>
        /// <param name="baseUri">The root uri that is used to substitute the path from the full URI</param>
        /// <param name="fullUri">The uri that points to a file or folder below the uri</param>
        /// <returns>Returns an <see cref="Uri"/> that contains only the path fragment </returns>
        public static Uri ExtractPath(Uri baseUri, Uri fullUri)
        {
            if (null == baseUri)
            {
                throw new ArgumentNullException("baseUri");
            }

            if (null == fullUri)
            {
                throw new ArgumentNullException("fullUri");
            }

            if (AreEqual(baseUri, fullUri))
            {
                return new Uri("/", UriKind.Relative);
            }

            //normalize the baseUri first. This has to be done to ensure a proper comparison. 
            //The fullUri does not need to be normalized because we just want to remove a prefix
            var normalizedBaseUri = AppendTrailingSlash(baseUri.AbsoluteUri);

            if (normalizedBaseUri.Length >= fullUri.AbsoluteUri.Length)
            {
                //The baseUri string is longer than the full uri. Therefore the fullUri is not a subitem of the baseUri. 
                //It might be possible to navigate to the destination relative using .. elements. This is currently not supported though
                var message = string.Format(SubversionVCAdapterResource.Culture, SubversionVCAdapterResource.ItemNotChildOfParentExceptionMessage, fullUri, baseUri);
                throw new FormatException(message);
            }

            //TODO SVN is case sensitive. At the moment we ignore this fact. This might has to be addressed later
            if (!fullUri.AbsoluteUri.StartsWith(normalizedBaseUri, StringComparison.OrdinalIgnoreCase))
            {
                //The fullUri does not start with the base URI. Therefore the fullUri is not a subitem of the baseUri. 
                //It might be possible to navigate to the destination relative using .. elements. This is currently not supported though
                var message = string.Format(SubversionVCAdapterResource.Culture, SubversionVCAdapterResource.ItemNotChildOfParentExceptionMessage, fullUri, baseUri);
                throw new FormatException(message);
            }

            var fragment = fullUri.AbsoluteUri.Substring(normalizedBaseUri.Length - 1);
            return new Uri(fragment, UriKind.Relative);
        }

        /// <summary>
        /// Executes a semantical comparison between two uri objects. 
        /// This method normalizes the strings and compares them afterwards
        /// </summary>
        /// <param name="uri1">The first uri for the comparison</param>
        /// <param name="uri2">The second uri for the comparison</param>
        /// <returns>true if the uris are equal; false otherwise</returns>
        internal static bool AreEqual(Uri uri1, Uri uri2)
        {
            var normalized1 = AppendTrailingSlash(uri1.AbsoluteUri);
            var normalized2 = AppendTrailingSlash(uri2.AbsoluteUri);

            return normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Calculates the new uri for the item 
        /// </summary>
        /// <param name="item">The source item that has the full path to the svn server item</param>
        /// <param name="oldBaseUri">The old base uri</param>
        /// <param name="newBaseUri">The new base uri for the item</param>
        /// <returns></returns>
        internal static Uri RebaseUri(Uri item, Uri oldBaseUri, Uri newBaseUri)
        {
            if(AreEqual(item, oldBaseUri))
            {
                //The uri of the item is the same as the base uri. 
                //Therefore, we can just return the newBaseUri
                return newBaseUri;
            }

            //first, we are going to extract the relative fragment based on the old base uri
            var relativeUri = PathUtils.ExtractPath(oldBaseUri, item);

            //now we are going to append this fragment to the new base uri
            return PathUtils.Combine(newBaseUri, relativeUri.OriginalString);
        }

        /// <summary>
        /// This method analyzes the fragments of an URI objects. It removes the last fragments and returns this URI
        /// </summary>
        /// <param name="item">The item that has to be processed</param>
        /// <returns>The uri object that contains the full uri to the parent item in the reopository </returns>
        internal static Uri GetParent(Uri item)
        {
            if (null == item)
            {
                throw new ArgumentNullException("baseUri");
            }

            if (0 == item.Segments.Length)
            { 
                var message = string.Format(SubversionVCAdapterResource.ExtractParentNotPossibleException, item.AbsoluteUri);
                throw new UriFormatException(message);
            }

            var normalizedUri = item.AbsoluteUri.TrimEnd(Separator);
            return GetNormalizedPath(normalizedUri.Substring(0, normalizedUri.LastIndexOf(Separator)));
        }

        /// <summary>
        /// This method analyzes the fragments of a path string. It removes the last fragments and returns this string
        /// </summary>
        /// <param name="item">The item that has to be processed</param>
        /// <returns>The string object that contains the full path to the parent item in the reopository </returns>
        internal static string GetParent(string item)
        {
            if (null == item)
            {
                throw new ArgumentNullException("item");
            }

            if (0 == item.Length)
            {
                var message = string.Format(SubversionVCAdapterResource.ExtractParentNotPossibleException, item);
                throw new UriFormatException(message);
            }

            var normalizedPath = item.TrimEnd(Separator);
            return normalizedPath.Substring(0, normalizedPath.LastIndexOf(Separator));
        }

        /// <summary>
        /// This method analyzes the fragment and retunrs the name of the actual item
        /// <example>
        /// item; svn://localhost/svn2tfs/Folder/File1.txt
        /// result: File1.txt
        /// </example>
        /// </summary>
        /// <param name="item">The full server path to the item</param>
        /// <returns>The name of the item</returns>
        public static string GetItemName(Uri item)
        {
            if (null == item)
            {
                throw new ArgumentNullException("item");
            }

            if (0 == item.Segments.Length)
            {
                var message = string.Format(SubversionVCAdapterResource.ExtractItemNameNotPossibleException, item.AbsoluteUri);
                throw new UriFormatException(message);
            }

            var normalizedUri = item.AbsoluteUri.TrimEnd(Separator);
            return normalizedUri.Substring(normalizedUri.LastIndexOf(Separator) + 1);
        }

        /// <summary>
        /// This method analyzes the fragment and retunrs the name of the actual item
        /// <example>
        /// item; svn://localhost/svn2tfs/Folder/File1.txt
        /// result: File1.txt
        /// </example>
        /// </summary>
        /// <param name="item">The full server path to the item</param>
        /// <returns>The name of the item</returns>
        public static string GetItemName(String item)
        {
            if (string.IsNullOrEmpty(item))
            {
                throw new ArgumentNullException("item");
            }

            var normalizedPath = item.TrimEnd(Separator);
            return normalizedPath.Substring(normalizedPath.LastIndexOf(Separator) + 1);
        }
    }
}
