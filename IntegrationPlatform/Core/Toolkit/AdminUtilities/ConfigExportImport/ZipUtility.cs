// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// This utility class implements basic zip archiving functionality
    /// </summary>
    public class ZipUtility
    {
        private const string RelationshipType = "TFSIntegrationAdminZipPackage";

        /// <summary>
        /// Creates a package zip file containing specified xml documents.
        /// </summary>
        public static void Zip(string outputZipFileName, string[] contentXmlFiles)
        {
            Dictionary<string, Uri> uris = new Dictionary<string, Uri>();

            foreach (string file in contentXmlFiles)
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri(file, UriKind.Relative));
                uris.Add(file, uri);
            }
            
            using (Package package = Package.Open(outputZipFileName, FileMode.Create))
            {
                foreach (var f in uris)
                {
                    // Create a package part
                    PackagePart packagePartDocument =
                        package.CreatePart(f.Value, System.Net.Mime.MediaTypeNames.Text.Xml);

                    // Copy the data to the Document Part
                    using (FileStream fileStream = new FileStream(f.Key, FileMode.Open))
                    {
                        CopyStream(fileStream, packagePartDocument.GetStream());
                    }

                    // Add a Package Relationship to the Document Part
                    package.CreateRelationship(packagePartDocument.Uri,
                                               TargetMode.Internal,
                                               RelationshipType);
                }
            }
        }

        /// <summary>
        ///   Extracts content and resource parts from a given Package
        ///   zip file to a specified target directory.</summary>
        /// <param name="packagePath">The relative path and filename of the Package zip file.</param>
        /// <param name="targetDirectory">The path to the targer folder.</param>
        public static void Unzip(string packagePath, string targetDirectory, bool cleanUpTargetDir)
        {
            // Create a new Target directory.
            DirectoryInfo directoryInfo = new DirectoryInfo(targetDirectory);
            if (directoryInfo.Exists && cleanUpTargetDir)
            {
                directoryInfo.Delete(true);
            }
            directoryInfo.Create();

            // Open the Package.
            using (Package package = Package.Open(packagePath, FileMode.Open, FileAccess.Read))
            {
                // Get the Package Relationships and look for the Document part based on the RelationshipType
                Uri docUri = null;
                foreach (PackageRelationship relationship in
                    package.GetRelationshipsByType(RelationshipType))
                {
                    // Resolve the Relationship Target Uri
                    docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), relationship.TargetUri);

                    // Open the Document Part, write the contents to a file.
                    PackagePart documentPart = package.GetPart(docUri);
                    ExtractPart(documentPart, targetDirectory);
                }
            }
        }

        /// <summary>
        ///   Extracts a specified package part to a target folder.
        /// </summary>
        /// <param name="packagePart">The package part to extract.</param>
        /// <param name="targetDirectory">The target directory to store the extracted file</param>
        private static void ExtractPart(PackagePart packagePart, string targetDirectory)
        {
            // fake a child folder path to build the correct absolute uri
            string pathToTarget = Path.Combine(targetDirectory, "childFolder"); 

            string stringPart = packagePart.Uri.ToString().TrimStart('/');
            Uri partUri = new Uri(stringPart, UriKind.Relative);

            // Create a full Uri to the Part
            Uri uriFullPartPath = new Uri(new Uri(pathToTarget, UriKind.Absolute), partUri);

            // Create the necessary Directories based on the Full Part Path
            Directory.CreateDirectory(Path.GetDirectoryName(uriFullPartPath.LocalPath));

            // Create the file with the Part content
            using (FileStream fileStream = new FileStream(uriFullPartPath.LocalPath, FileMode.Create))
            {
                CopyStream(packagePart.GetStream(), fileStream);
            }
        }

        /// <summary>
        /// Copies data from a source stream to a target stream.
        /// </summary>
        /// <param name="source">The source stream to copy from.</param>
        /// <param name="target">The destination stream to copy to.</param>
        private static void CopyStream(Stream source, Stream target)
        {
            const int bufSize = 0x1000;
            byte[] buf = new byte[bufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
            {
                target.Write(buf, 0, bytesRead);
            }
        }
    }
}
