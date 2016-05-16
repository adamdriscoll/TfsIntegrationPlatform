// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter.Properties;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.TfsVCAdapterCommon;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    public abstract class TfsCommonVCShellAdapter : TfsCommonShellAdapter
    {
        protected const string c_vcFilterStringPrefix = "$/";
        
        private static List<IConflictTypeView> s_conflictTypes;

        static TfsCommonVCShellAdapter()
        {
            s_conflictTypes = new List<IConflictTypeView>();
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCBranchParentNotFoundConflictType().ReferenceName,
                FriendlyName = Resources.VCBranchParentNotFoundConflictTypeFriendlyName,
                Description = Resources.VCBranchParentNotFoundConflictTypeDescription,
                Type = typeof(BranchParentNotFoundConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCContentConflictType().ReferenceName,
                FriendlyName = Resources.VCContentConflictTypeFriendlyName,
                Description = Resources.VCContentConflictTypeDescription,
                Type = typeof(VCContentConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCFilePropertyCreationConflictType().ReferenceName,
                FriendlyName = Resources.VCFilePropertyCreationConflictTypeFriendlyName,
                Description = Resources.VCFilePropertyCreationConflictTypeDescription,
                Type = typeof(FilePropertyCreationConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCInvalidLabelNameConflictType().ReferenceName,
                FriendlyName = Resources.VCInvalidLabelNameConflictTypeFriendlyName,
                Description = Resources.VCInvalidLabelNameConflictTypeDescription,
                Type = typeof(InvalidLabelNameConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCLabelAlreadyExistsConflictType().ReferenceName,
                FriendlyName = Resources.VCLabelAlreadyExistsConflictTypeFriendlyName,
                Description = Resources.VCLabelAlreadyExistsConflictTypeDescription,
                Type = typeof(DuplicateLabelNameConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCLabelCreationConflictType().ReferenceName,
                FriendlyName = Resources.VCLabelCreationConflictTypeFriendlyName,
                Description = Resources.VCLabelCreationConflictTypeDescription,
                Type = typeof(LabelCreationConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCMissingItemConflictType().ReferenceName,
                FriendlyName = Resources.VCMissingItemConflictTypeFriendlyName,
                Description = Resources.VCMissingItemConflictTypeDescription,
                Type = typeof(VCMissingItemConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCPathNotMappedConflictType().ReferenceName,
                FriendlyName = Resources.VCPathNotMappedConflictTypeFriendlyName,
                Description = Resources.VCPathNotMappedConflictTypeDescription,
                Type = typeof(VCPathNotMappedConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TfsCheckinConflictType().ReferenceName,
                FriendlyName = Resources.TfsCheckinConflictTypeFriendlyName,
                Description = Resources.TfsCheckinConflictTypeDescription,
                Type = typeof(TfsCheckinConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TFSDosShortNameConflictType().ReferenceName,
                FriendlyName = Resources.TFSDosShortNameConflictTypeFriendlyName,
                Description = Resources.TFSDosShortNameConflictTypeDescription,
                Type = typeof(InvalidShortFilenameFormatConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TFSHistoryNotFoundConflictType().ReferenceName,
                FriendlyName = Resources.TFSHistoryNotFoundConflictTypeFriendlyName,
                Description = Resources.TfsHistoryNotFoundConflictTypeDescription,
                Type = typeof(TfsHistoryNotFoundConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCInvalidPathConflictType().ReferenceName,
                FriendlyName = Resources.VCInvalidPathConflictTypeFriendlyName,
                Description = Resources.VCInvalidPathConflictTypeDescription,
                Type = typeof(VCInvalidPathConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TfsItemNotFoundConflictType().ReferenceName,
                FriendlyName = Resources.TfsItemNotFoundConflictTypeFriendlyName,
                Description = Resources.TfsItemNotFoundConflictTypeDescription,
                Type = typeof(TfsItemNotFoundConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new TFSZeroCheckinConflictType().ReferenceName,
                FriendlyName = Resources.TFSZeroCheckinConflictTypeFriendlyName,
                Description = Resources.UnChangedContentConflictTypeDescription,
                Type = typeof(UnchangedContentConflictTypeViewModel)
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCUserPromptConflictType().ReferenceName,
                FriendlyName = Resources.VCUserPromptConflictTypeFriendlyName,
                Description = Resources.VCUserPromptConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCChangeGroupInProgressConflictType().ReferenceName,
                FriendlyName = Resources.VCChangeGroupInProgressConflictTypeFriendlyName,
                Description = Resources.VCChangeGroupInProgressConflictTypeDescription
            });
            s_conflictTypes.Add(new ConflictTypeView
            {
                Guid = new VCNameSpaceContentConflictType().ReferenceName,
                FriendlyName = Resources.VCNameSpaceConflictTypeFriendlyName,
                Description = Resources.VCNameSpaceConflictTypeDescription,
                Type = typeof(VCNamespaceConflictTypeViewModel)
            });
        }

        public override IEnumerable<IConflictTypeView> GetConflictTypeViews()
        {
            return base.GetConflictTypeViews().Concat(s_conflictTypes);
        }
    }
}
