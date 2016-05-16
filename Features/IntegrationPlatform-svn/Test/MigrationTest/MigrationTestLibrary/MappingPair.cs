// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

namespace MigrationTestLibrary
{
    public class MappingPair
    {
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string SourceSnapshotStartPoint { get; set; }
        public string TargetSnapshotStartPoint { get; set; }
        public string SourcePeerSnapshotStartPoint { get; set; }
        public string TargetPeerSnapshotStartPoint { get; set; }
        public string SourceMergeScope { get; set; }
        public string TargetMergeScope { get; set; }
        public bool Cloak { get; set; }

        public MappingPair(string sourcePath, string targetPath) 
            : this (sourcePath, targetPath, false)
        {
        }

        public MappingPair(string sourcePath, string targetPath, bool cloak)
            : this (sourcePath, targetPath, cloak, null, null)
        {
        }

        public MappingPair(string sourcePath, string targetPath, bool cloak, string sourceSnapshotStartPoint, string targetSnapshotStartPoint)
            : this(sourcePath, targetPath, cloak, sourceSnapshotStartPoint, targetSnapshotStartPoint, null, null, null, null)
        {
        }

        public MappingPair(string sourcePath, string targetPath, bool cloak, string sourceSnapshotStartPoint, string targetSnapshotStartPoint,
            string sourceMergeScope, string targetMergeScope)
            : this(sourcePath, targetPath, cloak, sourceSnapshotStartPoint, targetSnapshotStartPoint,
            sourceMergeScope, targetMergeScope, null, null)
        {
        }

        public MappingPair(string sourcePath, string targetPath, bool cloak, string sourceSnapshotStartPoint, string targetSnapshotStartPoint,
            string sourceMergeScope, string targetMergeScope, string sourcePeerSnapshotStartPoint, string targetPeerSnapshotStartPoint)
        {
            SourcePath = sourcePath;
            TargetPath = targetPath;
            Cloak = cloak;
            SourceSnapshotStartPoint = sourceSnapshotStartPoint;
            TargetSnapshotStartPoint = targetSnapshotStartPoint;
            SourcePeerSnapshotStartPoint = sourcePeerSnapshotStartPoint;
            TargetPeerSnapshotStartPoint = targetPeerSnapshotStartPoint;
            SourceMergeScope = sourceMergeScope;
            TargetMergeScope = targetMergeScope;
        }
    }
}
