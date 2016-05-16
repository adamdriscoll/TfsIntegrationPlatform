// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Xml;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// The common interface for all migration actions to implement.  Migration actions represent a single
    /// operation to perform on the migration target.
    /// </summary>
    public interface IMigrationAction
    {
        /// <summary>
        /// The change group the action is a part of.
        /// </summary>
        ChangeGroup ChangeGroup
        {
            get;
            set;
        }

        /// <summary>
        /// The internal id of the action.
        /// </summary>
        long ActionId
        {
            get;
        }

        /// <summary>
        /// The source item of the migration action.  This is the item from the source system.
        /// The primary purpose of this item is for item download information.  If no item is being
        /// downloaded (for example during a delete) then this item may be null.
        /// </summary>
        IMigrationItem SourceItem
        {
            get;
        }

        /// <summary>
        /// The from path of the migration action.  This path is used during
        /// migration operations that involve a from and to path such as a rename, merge or branch.
        /// This item may be null if not needed (for example add, edit, delete and Label).
        /// </summary>
        string FromPath
        {
            get;
        }

        /// <summary>
        /// The path of the migration action. This item should never be null.
        /// </summary>
        string Path
        {
            get;
        }

        /// <summary>
        /// The action being performed.
        /// </summary>
        Guid Action
        {
            get;
        }

        /// <summary>
        /// If the action is a Label operation this is the name of the label.
        /// </summary>
        string Label
        {
            get;
        }

        /// <summary>
        /// If the action is an encoding operation this is the new encoding type.
        /// </summary>
        string Encoding
        {
            get;
        }

        /// <summary>
        /// If true the action is performed recursively, otherwise not.
        /// </summary>
        bool Recursive
        {
            get;
            set;
        }

        /// <summary>
        /// The order in which this action should be executed relative to other actions in the change group.
        /// </summary>
        int Order
        {
            get;
            set;
        }

        /// <summary>
        /// The current state of the action.
        /// </summary>
        ActionState State
        {
            get;
            set;
        }

        /// <summary>
        /// Type of the action item. 
        /// </summary>
        string ItemTypeReferenceName
        {
            get;
        }

        /// <summary>
        /// This Property is only used for undelete, branch and merge action. 
        /// For undelete action, this is the version in which the item was deleted.
        /// For branch action, this is the branch from version
        /// For merge action, this is the start version of merge
        /// </summary>
        string Version
        {
            get;
        }

        /// <summary>
        /// This Property is only used for merge action. 
        /// For merge action, this is the end version of merge
        /// </summary>
        string MergeVersionTo
        {
            get;
        }

        XmlDocument MigrationActionDescription
        {
            get;
        }
    }
}
