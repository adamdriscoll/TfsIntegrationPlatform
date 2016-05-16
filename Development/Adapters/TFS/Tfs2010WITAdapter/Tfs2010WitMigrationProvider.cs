// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Diagnostics;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public partial class Tfs2010WitMigrationProvider : TfsWITMigrationProvider
    {
        public Tfs2010WitMigrationProvider()
        { }

        protected override TfsMigrationDataSource InitializeMigrationDataSource()
        {
            return new Tfs2010MigrationDataSource();
        }

        protected override void UpdateSchemaNamespace(System.Xml.XmlDocument xmlDocument)
        {
            XmlNode witdNode = xmlDocument.DocumentElement.FirstChild;
            var witdAtt = witdNode.Attributes["xmlns:witd"];
            Debug.Assert(null != witdAtt, "Witd:WITD xmlns:witd attribute is not found");

            witdAtt.Value = "http://schemas.microsoft.com/VisualStudio/2008/workitemtracking/typedef";
        }
    }
}