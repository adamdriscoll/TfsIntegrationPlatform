// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter
{
    public interface IWITDSyncer
    {
        void DeleteNode(string xpathToRemovingNode);
                    
        void InsertNode(string xpathToParentNode, string nodeContent);
        
        void InsertNode(string xpathToParentNode, string nodeContent, string xpathToCheckDuplicate);

        void ReplaceNode(string xpathToReplacedNode, string xmlContentOfNewNode);

        void AddAttribute(string xpathToNode, string newAttribute, string attributeValue);

        void Sync();
    }
}
