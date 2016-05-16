// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.IO;

namespace MigrationTestLibrary
{
    public class MigrationItemStrings
    {
        private string name;
        private string localPath;
        private string serverPath;
        private string newName;
        private string newLocalPath;
        private string newServerPath;

        #region properties
        public string Name
        {
            get { return name; }
        }

        public string LocalPath
        {
            get { return localPath; }
        }

        public string ServerPath
        {
            get { return serverPath; }
        }

        public string NewName
        {
            get { return newName; }
        }

        public string NewLocalPath
        {
            get { return newLocalPath; }
        }

        public string NewServerPath
        {
            get { return newServerPath; }
        }
        #endregion

        /// <summary>
        /// Encapsulates all the paths for one item (file or folder) in a migration 
        /// </summary>
        /// <param name="itemName">The Desired itemName</param>
        /// <param name="newItemName">Item name after a namespace change (rename, branch)</param>
        /// <param name="env">MigrationTestEnvironment object</param>
        /// <param name="useSource">Determines if the paths should be based on the source of the mapping(tfs2tfs) or the target(wss2tfs)</param>
        public MigrationItemStrings(string itemName, string newItemName, MigrationTestEnvironment env, bool useSource)
        {
            if (useSource)
            {
                setStrings(itemName, newItemName, env.TestName, env.FirstSourceServerPath, env.SourceWorkspaceLocalPath);
            }
            else
            {
                setStrings(itemName, newItemName, env.TestName, env.FirstTargetServerPath, env.TargetWorkspaceLocalPath);
            }
        }

        /// <summary>
        /// Encapsulates all the paths for one item (file or folder) in a (WSS2TFS) migration 
        /// </summary>
        /// <param name="itemName">The Desired itemName</param>
        /// <param name="newItemName">Item name after a namespace change (rename, branch)</param>
        /// <param name="env">MigrationTestEnvironment object</param>
        /// <param name="useSource">Determines if the paths should be based on the source of the mapping(tfs2tfs) or that target(wss2tfs)</param>
        public MigrationItemStrings(string itemName, string newItemName, MigrationTestEnvironment env)
            : this(itemName, newItemName, env, false)
        {
        }

        /// <summary>
        /// Sets the private strings for this migration Item
        /// </summary>
        /// <param name="itemName">The Desired itemName</param>
        /// <param name="newItemName">Item name after a namespace change (rename, branch)</param>
        /// <param name="TestcaseName">The name of the testcase this item is migrated in</param>
        /// <param name="serverRoot">The path on the server where this item will be added</param>
        /// <param name="localRoot">The path on the hardrive where this item will be added</param>
        private void setStrings(string itemName, string newItemName, string testcaseName, string serverRoot, string localRoot)
        {
            name = itemName;
            localPath = Path.Combine(localRoot, itemName).Replace('/', '\\');
            serverPath = string.Format(TestConstants.urlCombine, serverRoot, itemName);

            if (newItemName != null)
            {
                newName = newItemName;
                newLocalPath = Path.Combine(localRoot, NewName).Replace('/', '\\');
                newServerPath = string.Format(TestConstants.urlCombine, serverRoot, NewName);
            }
        }
    }
}
