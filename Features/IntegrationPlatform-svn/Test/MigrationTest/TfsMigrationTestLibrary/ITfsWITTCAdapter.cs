// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrationTestLibrary;

namespace TfsMigrationTestLibrary
{
    public interface ITfsWITTestCaseAdapter : IWITTestCaseAdapter
    {
        int AddWorkItem(string type, string title, string desc, string areaPath);
        int GetHyperLinkCount(int workitemId);
        void AddRelatedWorkItemLink(int workItemId, int relatedWorkItemId);
        void DeleteRelatedWorkItemLink(int workItemId, int relatedWorkItemId);
        int GetRelatedLinkCount(int workItemId);
        void AddParentChildLink(int parentId, int childId);
        void AddParentChildLink(int parentId, int childId, bool isLocked);
        bool IsLinkLocked(int parentId, int childId);
        void UpdateLinkLockOption(int parentId, int childId, bool isLocked);
        void DeleteParentChildLink(int parentId, int childId);
    }
}
